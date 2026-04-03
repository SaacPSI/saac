// Licensed under the CeCILL-C License. See LICENSE.md file in the project root for full license information.
// This software is distributed under the CeCILL-C FREE SOFTWARE LICENSE AGREEMENT.
// See https://cecill.info/licences/Licence_CeCILL-C_V1-en.html for details.

#pragma once

#include "PsiWebsocketComponent.h"
#include "Delegates/DelegateCombinations.h"
#include "PsiStringComponent.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_TwoParams(FStringTimestampDelegate, const FString&, Message, double, OriginatingTime);

UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class SAAC_API UPsiStringComponent : public UPsiWebsocketComponent
{
	GENERATED_BODY()

public:
	// Sets default values for this component's properties
	UPsiStringComponent();

	UPROPERTY(BlueprintAssignable, Category = "Delegate")
	FStringTimestampDelegate StringMessageDelegate;
	
	// Sends a binary message with a .NET-tick timestamp and the given string through the websocket connection
	void SendBinaryMessage(const FString& Message);

protected:
	// Recieve a binary message with a .NET-tick timestamp and the given string from the websocket connection
	void OnBinaryMessage(const void* Data, SIZE_T Size, bool bIsLastFragment) override;

public:	
	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

private:
	float SendTimer = 0.0f;
};
