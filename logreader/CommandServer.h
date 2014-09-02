#pragma once

#ifndef __linux__
#define _WINSOCK_DEPRECATED_NO_WARNINGS 1

#include <boost\asio.hpp>
#include <boost\shared_ptr.hpp>
#include <boost\asio\io_service.hpp>
#else
#define _WINSOCK_DEPRECATED_NO_WARNINGS 1

#include <boost/asio.hpp>
#include <boost/shared_ptr.hpp>
#include <boost/asio/io_service.hpp>
#endif
class CommandServer
{

	CommandServer();

	void Run();
	boost::shared_ptr<boost::asio::ip::tcp::socket> _sock;
	boost::shared_ptr<boost::asio::ip::tcp::acceptor> _acceptor;
protected:
	// This is single CommandSErver instance
	static CommandServer* workserv;
	static int LinkCount;
	
public:
	
	virtual ~CommandServer();
	// Return instance of workserver or create it if need
	
	static CommandServer& Instance();
	// Return count of using of workserv pointer
	static const int& GetLinkCount();
	// Release workserv or decrement counter of workserv usage
	
	// Start listen for income command (create socket->accept->listen)
	bool StartListen();
protected:
	
	// This service object for asio
	boost::asio::io_service main_service;
public:
	// this our handler for accepted calls - hear we work with clients
	int accept_clients(boost::shared_ptr<boost::asio::ip::tcp::socket> sptr,const boost::system::error_code& ec);
	// Start wait for async operations
	
	// accept connection
	void do_accept(boost::shared_ptr<boost::asio::ip::tcp::socket> sock);
	// hear we get command and do action
	int WorkWithClient(boost::shared_ptr<boost::asio::ip::tcp::socket> sptr);
};

