// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "PsiWebsocketsManager.h"
#include "PsiWebsocketComponent.generated.h"

UCLASS( Abstract )
class SAAC_API UPsiWebsocketComponent : public UActorComponent
{
	GENERATED_BODY()

public:		
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "WebSocket")
	FString Topic = TEXT("Topic");
	// Sets default values for this component's properties
	UPsiWebsocketComponent();

protected:
	// Called when the game starts
	virtual void BeginPlay() override;

	// Basic websocket event handlers that can be overridden in derived classes to handle connection, disconnection, and message reception events. The base implementation will just log the events for debugging purposes.
	virtual void OnConnected();
	virtual void OnConnectionError(const FString& error);
	virtual void OnClosed(int32 StatusCode, const FString& Reason, bool bWasClean);
	virtual void OnRawMessage(const void* Data, int32 Size, int32 BytesRemaining);
	virtual void OnTextMessage(const FString& Data);
	virtual void OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment);

	// Appends the current time as .NET ticks (int64, 8 bytes, little-endian) to the send buffer
	void WriteTick(TArray<uint8>& Buffer) const;

	// Reads 8 bytes from the receive buffer as .NET ticks, advances Offset, and returns the
	// corresponding Unix timestamp in seconds (100-nanosecond intervals since Jan 1, 0001).
	// Returns 0.0 if the buffer does not contain enough data.
	double ReadTick(const uint8* Bytes, SIZE_T Size, SIZE_T& Offset) const;

protected:
	TWeakObjectPtr<APsiWebsocketsManager> WebSocketsManager;
	TSharedPtr<IWebSocket> WebSocket;
};
