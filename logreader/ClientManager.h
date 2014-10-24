#pragma once
#ifndef __linux__
#define _WINSOCK_DEPRECATED_NO_WARNINGS 1
#include <boost\asio.hpp>
#include <boost\shared_ptr.hpp>
#include <boost\iostreams\stream.hpp>
#include <boost\thread.hpp>
#include <boost\date_time\posix_time\posix_time.hpp>
#include <boost\property_tree\ptree.hpp>
#else
#define _WINSOCK_DEPRECATED_NO_WARNINGS 1

#include <boost/asio.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/iostreams/stream.hpp>
#include <boost/thread.hpp>
#include <boost/date_time/posix_time/posix_time.hpp>
#include <boost/property_tree/ptree.hpp>
#endif
#include <string>
#include <fstream>
class ClientManager
{
	const std::string STARTPACKETSIGNATURE = "<------------->";

	const std::string ENDSESSIONSIGNATURE = "Really destroying SIP dialog";
	const std::string ENDCALLLOG = "afterprocess.php";
	enum{ maxlength = 1024 };
	char buf[maxlength];
	ClientManager();
	boost::shared_ptr<boost::asio::ip::tcp::socket> clientsock;
	std::ifstream log;
	std::string LogPath;
	std::streamoff filesize;
	void SendError();
	int SendSipPacket(int sendcounter);
	int SetPositionToBeginSipHeader(int& outsendcounter);
public:
	int ReadCallSIP(boost::property_tree::ptree &pt);
	int ReadCallPBX(boost::property_tree::ptree &pt);
	int FindText(boost::property_tree::ptree &pt);
	int GetNextCall();
	static int LineBack(std::ifstream& log);
	static int setposition(std::string filename, std::string time, std::ifstream& log,std::streamoff& size);
	static boost::posix_time::ptime positonnewline(std::ifstream& log,
		char* data, char* tmptime, boost::posix_time::ptime& stoptime);
	ClientManager(boost::shared_ptr<boost::asio::ip::tcp::socket> sock);
	virtual ~ClientManager();
	// This function only start execution of clientmanager
	void Start();
	// Handler for read - hear come initiation of command session
	void WaitForCommand(boost::system::error_code ec);
	// Ping command - send answer PONG on request Ping
	int Ping();
	// hear we posit read pointer to asked time and read data
	int Read(boost::property_tree::ptree& pt);
	int Find(boost::property_tree::ptree& pt);
	// Read back for n count lines
	int ReadBack(boost::property_tree::ptree& pt);
};

