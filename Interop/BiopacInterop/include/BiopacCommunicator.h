#pragma once

#define WIN32_LEAN_AND_MEAN

#include "Constants.h"
#include <iostream>
//#include <thread>
#include <winsock2.h>
#include <ws2tcpip.h>
#include "Utilities.h"
#include <vector>
#include <deque>
#include <chrono>
#include "DataQueue.h"
#include "ManagedObject.h"

#include <xmlrpc-c/base.hpp>
#include <xmlrpc-c/client_simple.hpp>
using namespace System;
using namespace System::Threading;

namespace BiopacInterop
{
	class BiopacCommunicator
	{
	private:
		int getMPUnitType();
		int changeDataConnectionHostname(std::string connectionHostname);
		int changeTransportType(std::string transportType);
		int changeDataConnectionMethod(std::string connectionMethod);
		std::vector<xmlrpc_c::value> getEnabledChannels(std::string channelType);
		int changeDataDeliveryEnabled(std::string channelType, int index, bool state);
		int changeDataConnectionPort(std::string channelType, int index, int port);
		int changeDataType(std::string channelType, int index, std::string dataType, std::string dataEndian);

		std::vector<xmlrpc_c::value> analogChannels;
		std::vector<xmlrpc_c::value> digitalChannels;
		std::vector<xmlrpc_c::value> calcChannels;

		std::vector<short> analogData;
		std::vector<short> digitalData;
		std::vector<double> calcData;
		std::vector<double> dataTime;

		std::chrono::high_resolution_clock::time_point timepoint1, timepoint2, timepoint3;
        bool isSyncOnly;

	public:

		BiopacCommunicator(const bool& isSyncOnly);
		void setupCommunication();
		int toggleAcquisition();
		int startTcpServer();
		int getAcquisitionInProgress();
        bool isSynchOnly() const;

		int getData();
        int getAnalogData(const int& index);
        int getDigitalData(const int& index);
        double getCalcData(const int& index);
        double getDataTimeData(const int& index);
		void startCommunication();

		~BiopacCommunicator();

	};

	public ref class BiopacCommunicatorWrapper : ManagedObject::ManagedObject<BiopacInterop::BiopacCommunicator>
	{
	public:
		BiopacCommunicatorWrapper(bool isSynchOnly)
			: ManagedObject(new BiopacInterop::BiopacCommunicator(isSynchOnly))
		{

		}

		void StartCommunicationDelegateFunction()
		{
			m_Instance->startCommunication();
		}

		void StartCommunication()
		{
			ThreadStart^ startDelegate = gcnew ThreadStart(this, &BiopacCommunicatorWrapper::StartCommunicationDelegateFunction);
			Thread^ thread = gcnew Thread(startDelegate);
			thread->Start();
			System::Threading::Thread::Sleep(TimeSpan::FromSeconds(5));
            ToggleAcquisition();
		}

        void StartSyncedCommunication()
        {
            m_Instance->startCommunication();
            ToggleAcquisition();
        }

        bool ToggleAcquisition()
        {
            if (m_Instance->getAcquisitionInProgress() != 1)
            {
                if (m_Instance->toggleAcquisition() == 0)
                {
                    Console::WriteLine("XML-RPC SERVER: toggleAcquisition() SUCCEEDED" + "\n" + "....." + "acquisition_progress = on");
                    return true;
                }
            }
            else
            {
                m_Instance->toggleAcquisition();
                if (m_Instance->toggleAcquisition() == 0)
                {
                    Console::WriteLine("XML-RPC SERVER: toggleAcquisition() SUCCEEDED" + "\n" + "....." + "acquisition_progress = on");
                    return true;
                }
            }
            return false;
        }

		int GetData()
		{
			return m_Instance->getData();
		}

        int GetAnalogData(int index)
        {
            return m_Instance->getAnalogData(index);
        }

        int GetDigitalData(int index)
        {
            return m_Instance->getDigitalData(index);
        }

        double GetCalcData(int index)
        {
            return m_Instance->getCalcData(index);
        }

        double GetDataTimeData(int index)
        {
            return m_Instance->getDataTimeData(index);
        }

		int toggleAcquisition()
		{
			return m_Instance->toggleAcquisition();
		}

		int getAcquisitionInProgress()
		{
			return m_Instance->getAcquisitionInProgress();
		}
	};
}

