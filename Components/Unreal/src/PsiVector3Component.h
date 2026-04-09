// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "PsiWebsocketComponent.h"
#include "Delegates/DelegateCombinations.h"
#include "PsiVector3Component.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FVectorTimestampDelegate, const FVector&, Message, double, OriginatingTime);

UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class SAAC_API UPsiVector3Component : public UPsiWebsocketComponent
{
	GENERATED_BODY()

public:	
	// Sets default values for this component's properties
	UPsiVector3Component();

	UPROPERTY(BlueprintAssignable, Category = "Delegate")
	FVectorTimestampDelegate VectorMessageDelegate;

	// Sends a binary message with a .NET-tick timestamp and the given string through the websocket connection
	void SendBinaryMessage(const FVector& Message);

protected:
	// Manage the reception of binary messages, extract the timestamp and string from the buffer, then print the string with the timestamp in seconds since Jan 1, 1970 (Unix epoch) in the log
	void OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment);

public:
	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

private:
	float SendTimer = 0.0f;
};
