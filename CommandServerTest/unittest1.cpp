#include "stdafx.h"
#include "CppUnitTest.h"
#include "../logreader/CommandServer.h"
#include <boost\asio.hpp>
#include <boost\thread.hpp>
#include <boost\exception_ptr.hpp>

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace CommandServerTest
{		
	
	
	TEST_CLASS(CommandServerTest)
	{
	public:
		TEST_METHOD(GetInstanceReferent)
		{
			const CommandServer* a = &CommandServer::Instance();
			const CommandServer* b = &CommandServer::Instance();

			Assert::AreEqual(true, a == b, L"GetServInstance return different object instead of one workserv");

		}

		TEST_METHOD(CommandServerEchoTest)
		{
			
			boost::asio::io_service io_service;
			boost::asio::ip::tcp::socket sock(io_service);
			char testbuf[120];
			sock.connect(boost::asio::ip::tcp::endpoint(boost::asio::ip::address::from_string("127.0.0.1"), 2743));
			try
			{ 
				
				memset(testbuf, 0, strlen(testbuf));
				strcpy_s(testbuf, "testingstr");
				int len = strlen(testbuf);
				boost::asio::write(sock,boost::asio::buffer(testbuf, len));
				memset(testbuf, 0, strlen(testbuf));
				sock.read_some(boost::asio::buffer(testbuf, 20));
			}
			catch (std::exception& e)
			{
				memset(testbuf, 0, 120);
				std::string what =  e.what();
				std::string info = boost::diagnostic_information(e);

				
			}
			Assert::AreEqual(0, strncmp("testingstr",testbuf,strlen(testbuf)));
			
		}
		

		TEST_METHOD(CommandServerLinkCount)
		{
			int start = CommandServer::GetLinkCount();
			
			const CommandServer& a = CommandServer::Instance();
			int shouldbestartplus1 = CommandServer::GetLinkCount();
			Assert::AreEqual(1, shouldbestartplus1 - start, L"We Get Serv Instance one, but LinkCount not equal to one");
			
			{
				const CommandServer& a = CommandServer::Instance();
				int shouldbe2 = CommandServer::GetLinkCount();
				Assert::AreEqual(2, shouldbe2-start, L"INCREMENT Link Count ERROR");
			}

			int shouldbetwominusone = CommandServer::GetLinkCount();
			Assert::AreEqual(2, shouldbetwominusone-start, L"ERROR DECREMENT Link Count In destructor");
		}
		void TEsT_METHOD();
	};
}


