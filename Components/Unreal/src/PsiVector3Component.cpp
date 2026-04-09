// Fill out your copyright notice in the Description page of Project Settings.


#include "PsiVector3Component.h"
#include "IWebSocket.h"

// Sets default values for this component's properties
UPsiVector3Component::UPsiVector3Component()
{
}

// Called every frame
void UPsiVector3Component::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// Call SendBinaryMessage with a test string every second
	SendTimer += DeltaTime;
	if (SendTimer >= 1.0f)
	{
		SendTimer -= 1.0f;
		SendBinaryMessage(GetOwner()->GetActorLocation());
	}
}

void UPsiVector3Component::SendBinaryMessage(const FVector& Message)
{
	if (!WebSocket.IsValid() || !WebSocket->IsConnected())
	{
		return;
	}

	// Build the buffer: 8 bytes timestamp + 3 x 4 bytes floats (X, Y, Z)
	TArray<uint8> Buffer;
	Buffer.Reserve(sizeof(int64) + 3 * sizeof(float));

	// Timestamp as .NET ticks (100-nanosecond intervals since Jan 1, 0001), little-endian
	WriteTick(Buffer);

	// Narrow FVector double components to float to match C# BinaryReader.ReadSingle()
	const float X = static_cast<float>(Message.X);
	const float Y = static_cast<float>(Message.Y);
	const float Z = static_cast<float>(Message.Z);
	Buffer.Append(reinterpret_cast<const uint8*>(&X), sizeof(float));
	Buffer.Append(reinterpret_cast<const uint8*>(&Y), sizeof(float));
	Buffer.Append(reinterpret_cast<const uint8*>(&Z), sizeof(float));

	WebSocket->Send(Buffer.GetData(), Buffer.Num(), true);
}

void UPsiVector3Component::OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment)
{
	if (Data == nullptr || Size < sizeof(int64) + 3 * sizeof(float)) // 8 bytes timestamp + 12 bytes for X, Y, Z
	{
		return;
	}

	const uint8* Bytes = static_cast<const uint8*>(Data);
	SIZE_T Offset = 0;

	// Extract .NET ticks and convert to Unix timestamp (seconds since Jan 1, 1970)
	const double UnixTimestamp = ReadTick(Bytes, Size, Offset);
	if (UnixTimestamp == 0.0 && Offset == 0)
	{
		return;
	}
	// Read three single-precision floats matching C# BinaryWriter.Write(float), widen to double for FVector
	float X = 0.0f, Y = 0.0f, Z = 0.0f;
	FMemory::Memcpy(&X, Bytes + Offset, sizeof(float)); Offset += sizeof(float);
	FMemory::Memcpy(&Y, Bytes + Offset, sizeof(float)); Offset += sizeof(float);
	FMemory::Memcpy(&Z, Bytes + Offset, sizeof(float));
	const FVector Vector(static_cast<double>(X), static_cast<double>(Y), static_cast<double>(Z));

	VectorMessageDelegate.Broadcast(Vector, UnixTimestamp);
	UE_LOG(LogTemp, Log, TEXT("%s recieve [%.3f] (%.3f, %.3f, %.3f)"), *Topic, UnixTimestamp, Vector.X, Vector.Y, Vector.Z);
}