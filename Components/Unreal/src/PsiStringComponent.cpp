// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

#include "PsiStringComponent.h"
#include "IWebSocket.h"

// Sets default values for this component's properties
UPsiStringComponent::UPsiStringComponent()
{
}

// Called every frame
void UPsiStringComponent::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// Call SendBinaryMessage with a test string every second
	SendTimer += DeltaTime;
	if (SendTimer >= 1.0f)
	{
		SendTimer -= 1.0f;
		SendBinaryMessage(TEXT("Hello from Unreal"));
	}
}

void UPsiStringComponent::SendBinaryMessage(const FString& Message)
{
	if (!WebSocket.IsValid() || !WebSocket->IsConnected())
	{
		return;
	}

	// Convert to UTF-8
	FTCHARToUTF8 Converter(*Message);
	const uint8* StringData = reinterpret_cast<const uint8*>(Converter.Get());
	int32 StringLength = Converter.Length();

	// Write the string length as a 7-bit encoded integer (max 5 bytes for a 32-bit int)
	uint8 EncodedLength[5];
	int32 EncodedLengthBytes = 0;
	int32 LengthValue = StringLength;
	while (LengthValue >= 0x80)
	{
		EncodedLength[EncodedLengthBytes++] = static_cast<uint8>((LengthValue & 0x7F) | 0x80);
		LengthValue >>= 7;
	}
	EncodedLength[EncodedLengthBytes++] = static_cast<uint8>(LengthValue);

	// Build the buffer: 8 bytes timestamp + encoded length + string bytes
	TArray<uint8> Buffer;
	Buffer.Reserve(8 + EncodedLengthBytes + StringLength);

	// Timestamp as .NET ticks (100-nanosecond intervals since Jan 1, 0001), little-endian
	WriteTick(Buffer);

	// 7-bit encoded string length
	Buffer.Append(EncodedLength, EncodedLengthBytes);

	// UTF-8 string bytes
	Buffer.Append(StringData, StringLength);

	WebSocket->Send(Buffer.GetData(), Buffer.Num(), true);
}

void UPsiStringComponent::OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment)
{
	if (Data == nullptr || Size < 9) // 8 bytes timestamp + at least 1 byte for the encoded length
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

	// Read 7-bit encoded string length
	int32 StringLength = 0;
	int32 Shift = 0;
	while (Offset < Size)
	{
		const uint8 Byte = Bytes[Offset++];
		StringLength |= (Byte & 0x7F) << Shift;
		if ((Byte & 0x80) == 0)
		{
			break;
		}
		Shift += 7;
	}

	// Extract UTF-8 string bytes into a null-terminated buffer for conversion
	if (Offset + StringLength > Size)
	{
		return;
	}
	TArray<ANSICHAR> StringBuffer;
	StringBuffer.SetNumUninitialized(StringLength + 1);
	FMemory::Memcpy(StringBuffer.GetData(), Bytes + Offset, StringLength);
	StringBuffer[StringLength] = '\0';
	const FString Message = UTF8_TO_TCHAR(StringBuffer.GetData());

	StringMessageDelegate.Broadcast(Message, UnixTimestamp);
	UE_LOG(LogTemp, Log, TEXT("%s recieve [%.3f] %s"), *Topic, UnixTimestamp, *Message);
}