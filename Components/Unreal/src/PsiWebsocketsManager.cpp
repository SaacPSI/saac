// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

#include "PsiWebsocketsManager.h"
#include "Runtime/Online/WebSockets/Public/WebSocketsModule.h"
#include "IWebSocket.h"

APsiWebsocketsManager::APsiWebsocketsManager()
{
	Protocols.Add(TEXT("bin"));
	UpgradeHeaders.Add(TEXT("Origin"), TEXT("http://localhost"));
	UpgradeHeaders.Add(TEXT("User-Agent"), TEXT("UnrealEngine"));

	if (!FModuleManager::Get().IsModuleLoaded("WebSockets"))
	{
		FModuleManager::Get().LoadModule("WebSockets");
	}
}

void APsiWebsocketsManager::ShutdownWebSockets()
{
	for (auto& Connection : WebSocketConnections)
	{
		if (Connection.Value.IsValid())
		{
			Connection.Value->Close();
			Connection.Value.Reset();
		}
	}
}

TSharedPtr<IWebSocket> APsiWebsocketsManager::CreateWebSocket(const FString& Topic)
{
	if (WebSocketConnections.Contains(Topic))
	{
		return WebSocketConnections[Topic];
	}
	FString URL = FString::Printf(TEXT("%s%s"), *WebSocketUrl, *Topic);
	TSharedPtr<IWebSocket> socket = FWebSocketsModule::Get().CreateWebSocket(URL);
	WebSocketConnections.Add(Topic, socket);
	return socket;
}