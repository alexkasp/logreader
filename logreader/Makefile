default:
	export CFLAGS="$CFLAGS -O0 -fbuiltin -g"
	export CXXFLAGS="$CXXFLAGS -O0 -fbuiltin -g"
	g++  main.cpp ClientManager.cpp CommandServer.cpp -o main -lpthread -std=c++0x -I /usr/local/boost/include/ -L /usr/local/boost/lib/ -Wl,-Bstatic -lboost_log -lboost_log_setup \
	-lboost_filesystem -lboost_system -lboost_regex -lboost_date_time -lboost_thread -Wl,-Bdynamic -lrt -lm -lcurl -ggdb
clean:
	rm -f ./main ./test ${EVENTREADERDIR}/eventreader.o ${EVENTREADERDIR}/parser.o ${LOGGERDIR}/logger.o ${LOGGERDIR}/logmanagerinterface.o ${LOGGERDIR}/logmanager.o