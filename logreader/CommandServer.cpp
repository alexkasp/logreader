

#include "CommandServer.h"
#include <iostream>
#ifdef __linux__
#include <boost/shared_ptr.hpp>
#include <boost/thread.hpp>
#include <boost/bind.hpp>
#else
#include <boost\shared_ptr.hpp>
#include <boost\thread.hpp>
#include <boost\bind.hpp>

#endif
#include "ClientManager.h"

CommandServer* CommandServer::workserv = nullptr;
 int CommandServer::LinkCount = 0;

CommandServer::CommandServer()
{
	
}


CommandServer::~CommandServer()
{
	--LinkCount;

	if ((LinkCount==0)&&(workserv != nullptr))
	{
		delete workserv;
	}
}





// Return count of using of workserv pointer
const int& CommandServer::GetLinkCount()
{
	return LinkCount;
}


 CommandServer& CommandServer::Instance()
{
	static CommandServer workserv;
	++LinkCount;
	return workserv;
}

// Start listen for income command (create socket->accept->listen)
bool CommandServer::StartListen()
{
	_acceptor = boost::shared_ptr<boost::asio::ip::tcp::acceptor>(new boost::asio::ip::tcp::acceptor(main_service, 
		boost::asio::ip::tcp::endpoint(boost::asio::ip::tcp::v4(), 2743)));
	_sock = boost::shared_ptr<boost::asio::ip::tcp::socket>(new boost::asio::ip::tcp::socket(main_service));
	
	do_accept(_sock);

	Run();
	return false;
}


// this our handler for accepted calls - hear we work with clients
int CommandServer::accept_clients(boost::shared_ptr<boost::asio::ip::tcp::socket> sptr,const boost::system::error_code& ec)
{
	if (!ec)
	{
		boost::thread t(boost::bind(&CommandServer::WorkWithClient,this,sptr));

	}
	//WorkWithClient(sptr);
	boost::shared_ptr<boost::asio::ip::tcp::socket> sockptr(new boost::asio::ip::tcp::socket(main_service));
	do_accept(sockptr);

	return 0;
}


// Start wait for async operations
void CommandServer::Run()
{
	main_service.run();
}


// accept connection
void CommandServer::do_accept(boost::shared_ptr<boost::asio::ip::tcp::socket> sock)
{
	_acceptor->async_accept(*sock, boost::bind(&CommandServer::accept_clients, this, sock, _1));
}


// hear we get command and do action
int CommandServer::WorkWithClient(boost::shared_ptr<boost::asio::ip::tcp::socket> sptr)
{
	std::cout<<"ACCEPT COnnection"<<std::endl;
	ClientManager* manager = new ClientManager(sptr);
	manager->Start();
	return 0;
}
