// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.
#pragma once

#include "CoreMinimal.h"
#include "GameFramework/WorldSettings.h"
#include "Runtime/Online/WebSockets/Public/IWebSocketsManager.h"
#include "PsiWebsocketsManager.generated.h"

/**
 * Class that handle websocket connections and management. It is implemented as a world settings to be able to use it in blueprints and have it available in the level. It implements IWebSocketsManager to be able to use it as the main manager for websocket connections in the project.
 */
UCLASS()
class SAAC_API APsiWebsocketsManager : public AWorldSettings
{
	GENERATED_BODY() 

public:
	// WebSocket server URL to connect to
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Globals")
	FString WebSocketUrl = TEXT("ws://localhost:8080/");

	APsiWebsocketsManager();

	// Close the connection and clean up any resources used by the manager (destructor) that call ShutdownWebSockets
	void ShutdownWebSockets();

	// Create a new web socket connection to the specified topic
	TSharedPtr<IWebSocket> CreateWebSocket(const FString& Url);

protected:
	TMap<FString, TSharedPtr<IWebSocket>> WebSocketConnections;
	TArray<FString> Protocols;
	TMap<FString, FString> UpgradeHeaders;
};
