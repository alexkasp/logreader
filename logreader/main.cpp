#include "CommandServer.h"


int main()
{
	CommandServer::Instance().StartListen();
	return 0;
}