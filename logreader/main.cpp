#include "CommandServer.h"

#define VERSION_NUMBER 0.1
int main()
{
	std::cout<<"VERSION_NUMBER="<<VERSION_NUMBER<<std::endl;
	CommandServer::Instance().StartListen();
	return 0;
}