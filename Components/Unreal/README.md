# Unreal

## Summary
Project containing Unreal Engine C++ classes to communicate with a [PSI](https://github.com/microsoft/psi) pipeline over WebSockets. Messages are serialized as binary frames with a .NET tick timestamp (int64, 8 bytes, little-endian), matching the format expected by [WebSocketSource](../InteropExtension/src/WebSocketSource.cs) and [WebSocketWriter](../InteropExtension/src/WebSocketWriter.cs) on the \psi side.

## Files
* [PsiWebsocketsManager](src/PsiWebsocketsManager.h) `AWorldSettings` subclass that acts as the central WebSocket manager for a level. Exposes the server URL (`ws://localhost:8080/` by default) as a Blueprint-editable property and provides `CreateWebSocket` / `ShutdownWebSockets` lifecycle helpers.
* [PsiWebsocketComponent](src/PsiWebsocketComponent.h) Abstract `UActorComponent` base class. Handles connection to `APsiWebsocketsManager`, exposes overridable event callbacks (`OnConnected`, `OnClosed`, `OnBinaryMessage`, ...) and provides shared helpers `WriteTick` / `ReadTick` to encode and decode .NET tick timestamps in send/receive buffers.
* [PsiStringComponent](src/PsiStringComponent.h) Concrete component for sending and receiving UTF-8 string messages. Exposes a `FStringTimestampDelegate` Blueprint-assignable delegate fired on reception with the decoded message and its originating time.
* [PsiVector3Component](src/PsiVector3Component.h) Concrete component for sending and receiving `FVector` (3 x float) messages. Exposes a `FVectorTimestampDelegate` Blueprint-assignable delegate fired on reception with the decoded vector and its originating time.

## Current issues

## Future works
More message types (e.g. images, quaternions, custom structs), Blueprint-friendly send functions, editor utilities to auto-place components based on level design, etc.
## Example

### Sending and receiving a string

Place a `UPsiStringComponent` on any actor. Set the `Topic` property to match the \psi stream name (e.g. `"HotTopic"`). Bind to the delegate to react to incoming messages:

```cpp
// BeginPlay of your Actor
UPsiStringComponent* StringComp = CreateDefaultSubobject<UPsiStringComponent>(TEXT("StringComp"));
StringComp->Topic = TEXT("HotTopic");
StringComp->StringMessageDelegate.AddDynamic(this, &AMyActor::OnStringReceived);

// Callback
void AMyActor::OnStringReceived(const FString& Message, double OriginatingTime)
{
    UE_LOG(LogTemp, Log, TEXT("Received '%s' at t=%.6f"), *Message, OriginatingTime);
}

// Sending
StringComp->SendBinaryMessage(TEXT("Hello PSI"));
```

The frame layout on the wire is: `[int64 .NET ticks (8 bytes LE)] [UTF-8 string bytes]`, matching `PsiFormatString` on the \psi side.

---

### Sending and receiving a Vector3

Place a `UPsiVector3Component` on any actor. Set the `Topic` property (e.g. `"HeadPosition"`). Bind to the delegate:

```cpp
UPsiVector3Component* VecComp = CreateDefaultSubobject<UPsiVector3Component>(TEXT("VecComp"));
VecComp->Topic = TEXT("HeadPosition");
VecComp->VectorMessageDelegate.AddDynamic(this, &AMyActor::OnVectorReceived);

// Callback
void AMyActor::OnVectorReceived(const FVector& Value, double OriginatingTime)
{
    UE_LOG(LogTemp, Log, TEXT("Received (%.2f, %.2f, %.2f) at t=%.6f"),
        Value.X, Value.Y, Value.Z, OriginatingTime);
}

// Sending
VecComp->SendBinaryMessage(GetActorLocation());
```

The frame layout on the wire is: `[int64 .NET ticks (8 bytes LE)] [float X (4 bytes LE)] [float Y (4 bytes LE)] [float Z (4 bytes LE)]`, matching `PsiFormatVector3` on the \psi side.
