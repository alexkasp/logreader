#define _CRT_SECURE_NO_WARNINGS
#include "ClientManager.h"


#ifdef BOOST_OS_WINDOWS

#include <boost\property_tree\json_parser.hpp>
#include <boost\bind.hpp>
#else
#include <boost/property_tree/json_parser.hpp>
#include <boost/bind.hpp>
#endif

#include <iostream>
#include <string>
#include <cstring>

int ClientManager::setposition(std::string filename,std::string time,std::ifstream& log)
{
	char data[2048];
	//CommandServer::Instance().StartListen();
	std::cout<<"Filename: "<<filename<<std::endl;
	if(!log.is_open())
	    log.open(filename);
	
	log.getline(data, 2048);

	char tmptime[22];
	strncpy(tmptime, &data[1], 19);
	boost::posix_time::ptime asktime = boost::posix_time::time_from_string(time);
	boost::posix_time::ptime stoptime;

	std::cout<<"Start positionning"<<std::endl;
	log.seekg(0, log.end);
	std::streamoff ptr = log.tellg();
	int step = 2;
	std::streamoff fullsize = ptr/step;
	log.seekg(fullsize, log.beg);
	ClientManager::positonnewline(log, data, tmptime, stoptime);
	while (stoptime != asktime)
	{
		step *= 2;
		std::streamoff findstep = ptr / step;
		if (findstep <= 1)
			return 0;
		std::cout << "stoptime = " << stoptime << " asktime = " << asktime << std::endl;
		if (stoptime > asktime)
		{
			fullsize = fullsize - findstep;
		}
		else//stoptime < asktime
		{
			fullsize = fullsize + findstep;
		}
		if((fullsize==0)||(fullsize>ptr-1000))
		    return 0;
		    std::cout<<"posit "<<fullsize<<std::endl;
		log.seekg(fullsize, log.beg);
		ClientManager::positonnewline(log, data, tmptime, stoptime);
		
	}

	
	return 1;
}

boost::posix_time::ptime ClientManager::positonnewline(std::ifstream& log, char* data, char* tmptime, boost::posix_time::ptime& stoptime)
{
	//std::cout<<"posit_to_new_line "<<std::endl;
	stoptime = boost::posix_time::time_from_string("1970-01-01 00:00:00");
	std::streamoff ptr = log.tellg();
	int position = 0;
	while (1)
	{
		log.seekg(ptr - 2, log.beg);
		ptr = log.tellg();

		char in = log.get();
		//std::cout<<"["<<ptr<<"]";
		if (in == '\n')
		{
			
			//get line from file until space is encountered and put it in data1
			log.getline(data, 2048);
			//std::cout<<data<<std::endl;
			if (data[0] != '[')
				continue;

			strncpy(tmptime, &data[1], 19);
			tmptime[19]=0;
			stoptime = boost::posix_time::time_from_string(tmptime);
			std::cout << stoptime << std::endl;
			return stoptime;
		}
	}
	return stoptime;
}

ClientManager::ClientManager()
{
}


ClientManager::~ClientManager()
{
}

ClientManager::ClientManager(boost::shared_ptr<boost::asio::ip::tcp::socket> sock) :
clientsock(sock)
{
#ifndef __linux__
	LogPath = "c:\\work\\";
#else
	LogPath = "/var/log/asterisk/";
#endif
}

// This function only start execution of clientmanager
void ClientManager::Start()
{	
	memset(buf, 0, maxlength);
	clientsock->async_receive(boost::asio::buffer(buf,maxlength),boost::bind(&ClientManager::WaitForCommand,this,_1));
}


// Handler for read - hear come initiation of command session
void ClientManager::WaitForCommand(boost::system::error_code ec)
{
	std::cout<<"Command income"<<std::endl;
	std::stringstream data;
	data << buf;
	if (!ec)
	{
		boost::property_tree::ptree pt;

		try{
			boost::property_tree::json_parser::read_json(data, pt);
			std::string Action = pt.get<std::string>("Action");
			if (Action == "Ping")
			{
				Ping();
			}
			else if (Action == "Find")
			{
				Find(pt);
			}
			else if (Action == "Read")
			{
				Read(pt);
			}
			else if (Action == "SetLogPath")
			{
				LogPath = pt.get<std::string>("LogPath");
				clientsock->write_some(boost::asio::buffer("OK",2));
			}
			else if (Action == "NextCall")
			{
				GetNextCall();
			}
			else
			{
				std::cout << "WRONG COMMAND" << std::endl;
				clientsock->write_some(boost::asio::buffer("WRONG_COMMAND",13));
			}
		}
		catch (std::exception& e)
		{

			std::string What = e.what();
			std::cout<<What<<std::endl;
			clientsock->write_some(boost::asio::buffer(What));
		}
	
		Start();
		return;
	}
	
	std::cout << "Error!!! we close socket" << std::endl;
	clientsock->close();
	return;
}

int ClientManager::GetNextCall()
{
	char data[8096];
	char tmptime[22];
	boost::posix_time::ptime ptime;
	const char INVITE_SIGNATURE[] = "INVITE sip:";
	int invite_signature_length = strlen(INVITE_SIGNATURE);
	while (log.getline(data, 8096))
	{
		if (strncmp(INVITE_SIGNATURE, &data[1], invite_signature_length) == 0)
		{
			clientsock->write_some(boost::asio::buffer(data, strlen(data)));
			
			return 1;
		}
	}
	
	return 0;
}

// Ping command - send answer PONG on request Ping
int ClientManager::Ping()
{
	clientsock->write_some(boost::asio::buffer("PONG", 4));
	return 1;
}


// hear we posit read pointer to asked time and read data
int ClientManager::Find(boost::property_tree::ptree& pt)
{
	std::string filename = pt.get<std::string>("Filename");
	std::string asktime = pt.get<std::string>("AskTime");
	if (ClientManager::setposition(LogPath + filename, asktime, log))
		clientsock->write_some(boost::asio::buffer("OK", 2));
	else
		clientsock->write_some(boost::asio::buffer("ERROR", 5));
	return 0;
}

int ClientManager::Read(boost::property_tree::ptree& pt)
{
	int linecount = pt.get<int>("LineCount");
	for (int i = 0; i < linecount; ++i)
	{
		if (log.getline(buf, maxlength))
		{
			clientsock->write_some(boost::asio::buffer(buf, strlen(buf)));
			clientsock->read_some(boost::asio::buffer(buf, maxlength));
		}
		else
			return 0;
	}
	return 0;
}