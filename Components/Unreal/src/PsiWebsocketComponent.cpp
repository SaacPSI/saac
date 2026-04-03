// Fill out your copyright notice in the Description page of Project Settings.


#include "PsiWebsocketComponent.h"
#include "IWebSocket.h"

// Sets default values for this component's properties
UPsiWebsocketComponent::UPsiWebsocketComponent()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;

	// Retrieve the websocket manager from the world settings
	if (UWorld* World = GetWorld())
	{
		WebSocketsManager = Cast<APsiWebsocketsManager>(World->GetWorldSettings());
	}
}

// Called when the game starts
void UPsiWebsocketComponent::BeginPlay()
{
	Super::BeginPlay();

	// Create a websocket connection with the manager and store it in a member variable for later use
	if (!WebSocketsManager.IsValid())
	{
		WebSocketsManager = Cast<APsiWebsocketsManager>(GetWorld()->GetWorldSettings());
	}

	if (WebSocketsManager.IsValid())
	{
		WebSocket = WebSocketsManager->CreateWebSocket(Topic);
		WebSocket->OnConnected().AddUObject(this, &UPsiWebsocketComponent::OnConnected);
		WebSocket->OnConnectionError().AddUObject(this, &UPsiWebsocketComponent::OnConnectionError);
		WebSocket->OnClosed().AddUObject(this, &UPsiWebsocketComponent::OnClosed);
		WebSocket->OnRawMessage().AddLambda([this](const void* Data, int32 Size, int32 BytesRemaining) {
			OnRawMessage(Data, Size, BytesRemaining);
			}); //.AddUObject(this, &UPsiWebsocketComponent::OnRawMessage);
		WebSocket->OnMessage().AddUObject(this, &UPsiWebsocketComponent::OnTextMessage);
		WebSocket->OnBinaryMessage().AddUObject(this, &UPsiWebsocketComponent::OnBinaryMessage);
		WebSocket->Connect();
	}
}

void UPsiWebsocketComponent::OnConnected()
{
	UE_LOG(LogTemp, Log, TEXT("WebSocket %s connection established."), *Topic);
}

void UPsiWebsocketComponent::OnConnectionError(const FString& error)
{
	UE_LOG(LogTemp, Warning, TEXT("WebSocket %s connection error: %s"), *Topic, *error);
}

void UPsiWebsocketComponent::OnClosed(int32 StatusCode, const FString& Reason, bool bWasClean)
{
	UE_LOG(LogTemp, Warning, TEXT("WebSocket %s closed. Code: %d, Reason: %s"), *Topic, StatusCode, *Reason);
}

void UPsiWebsocketComponent::OnRawMessage(const void* Data, int32 Size, int32 BytesRemaining)
{
	UE_LOG(LogTemp, Log, TEXT("WebSocket %s received Raw message (%d bytes)"), *Topic, Size);
}

void UPsiWebsocketComponent::OnTextMessage(const FString& Data)
{
	UE_LOG(LogTemp, Log, TEXT("WebSocket %s received message : %s"), *Topic, *Data);
}

void UPsiWebsocketComponent::OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment)
{
	UE_LOG(LogTemp, Log, TEXT("WebSocket %s received Binary message (%d bytes)"), *Topic, Size);
}

void UPsiWebsocketComponent::WriteTick(TArray<uint8>& Buffer) const
{
	const int64 Ticks = FDateTime::UtcNow().GetTicks();
	Buffer.Append(reinterpret_cast<const uint8*>(&Ticks), sizeof(int64));
}

double UPsiWebsocketComponent::ReadTick(const uint8* Bytes, SIZE_T Size, SIZE_T& Offset) const
{
	if (Bytes == nullptr || Offset + sizeof(int64) > Size)
	{
		return 0.0;
	}

	int64 Ticks = 0;
	FMemory::Memcpy(&Ticks, Bytes + Offset, sizeof(int64));
	Offset += sizeof(int64);

	static const int64 TicksAtUnixEpoch = 621355968000000000LL;
	static const int64 TicksPerSecond   = 10000000LL;
	return static_cast<double>(Ticks - TicksAtUnixEpoch) / static_cast<double>(TicksPerSecond);
}
