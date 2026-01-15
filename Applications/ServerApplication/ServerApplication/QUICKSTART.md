# ?? QuickStart Guide - Configuration Loader

## Démarrage Rapide (5 minutes)

### 1?? Préparation de votre DLL

**Créez un projet Class Library** dans Visual Studio :

```csharp
// MyDataClasses/SensorData.cs
using System;

namespace MyDataClasses
{
    public class SensorData
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

**Compilez** en `.dll` (ex: `bin/Release/MyDataClasses.dll`)

---

### 2?? Charger la DLL dans SAAC

Dans `MainWindow.xaml.cs`, appelez :

```csharp
LoadAssemblyFromFile();
```

**Résultat :**
- Une boîte de dialogue s'ouvre
- Sélectionnez votre `MyDataClasses.dll`
- Un fichier `config_template.json` est généré automatiquement

---

### 3?? Configurer le JSON

**Fichier généré** (`config_template.json`) :

```json
[
  {
    "topic": "sensordata",
    "type": "SensorData",
    "class": "MyDataClasses.SensorData, MyDataClasses",
    "transformer": null
  }
]
```

**Modifiez-le selon vos besoins** :

```json
[
  {
    "topic": "sensor_temperature",
    "type": "SensorData",
    "class": "MyDataClasses.SensorData, MyDataClasses",
    "transformer": null
  },
  {
    "topic": "sensor_humidity",
    "type": "SensorData",
    "class": "MyDataClasses.SensorData, MyDataClasses",
    "transformer": null
  }
]
```

---

### 4?? Charger la Configuration

Dans `MainWindow.xaml.cs`, appelez :

```csharp
LoadConfigurationFromJsonFile();
```

**Résultat :**
- Une boîte de dialogue s'ouvre
- Sélectionnez votre fichier JSON
- Les logs affichent les topics chargés

```
? Configuration JSON chargée : C:\config.json
  Topics configurés : sensor_temperature, sensor_humidity
```

---

## ? Vérification

Vérifiez que la configuration est chargée :

```csharp
// Dans le code-behind
foreach (var topic in configuration.TopicsTypes.Keys)
{
    AddLog($"Topic configuré : {topic}");
}
```

---

## ?? Cas d'Usage Typiques

### Cas 1 : Types Simples (Built-in)

```json
[
  {
    "topic": "temperature",
    "type": "float",
    "class": "System.Single, System.Private.CoreLib",
    "transformer": null
  }
]
```

### Cas 2 : Types Personnalisés

```json
[
  {
    "topic": "custom_data",
    "type": "MyClass",
    "class": "MyNamespace.MyClass, MyAssembly",
    "transformer": null
  }
]
```

### Cas 3 : Avec Transformer

```json
[
  {
    "topic": "raw_audio",
    "type": "byte[]",
    "class": "System.Byte[], System.Private.CoreLib",
    "transformer": "MyNamespace.AudioTransformer, MyAssembly"
  }
]
```

---

## ?? Dépannage

| Erreur | Cause | Solution |
|--------|-------|----------|
| "? Veuillez d'abord charger une DLL" | JSON sans DLL chargée | Charger la DLL en premier |
| "Type introuvable : XYZ" | Classe inexistante dans la DLL | Vérifier le nom qualifié complet |
| "La DLL n'a pas été trouvée" | Chemin invalide | Sélectionner le bon fichier .dll |
| "Le fichier JSON est vide" | JSON vide ou mal formaté | Valider le JSON |
| "Erreur de parsing JSON" | JSON mal formaté | Utiliser un validateur JSON |

---

## ?? Documentation Complète

Pour plus de détails, consultez :

- **`CONFIGURATION_LOADER_README.md`** : Guide détaillé complet
- **`IMPLEMENTATION_SUMMARY.md`** : Vue d'ensemble technique
- **`UI_INTEGRATION_GUIDE.md`** : Intégration avec l'interface
- **`EXAMPLE_DATA_CLASSES.cs`** : Exemples de code
- **`config_example.json`** : Exemple de configuration

---

## ?? Tips Utiles

1. **Générez le template automatiquement** - Ne le créez pas manuellement
2. **Validez votre JSON** - Utilisez un validateur en ligne si doute
3. **Vérifiez les noms qualifiés** - Utilisez `FullName` + assembly name
4. **Utilisez les logs** - Ils indiquent exactement ce qui s'est passé

---

## ?? Exemple Complet

```csharp
// MainWindow.xaml.cs

public partial class MainWindow : Window
{
    private void SetupConfiguration()
    {
        // Étape 1 : Charger la DLL
        LoadAssemblyFromFile(); // Sélectionne MyDataClasses.dll
        
        // Template JSON généré automatiquement
        
        // Étape 2 : Charger la configuration JSON
        LoadConfigurationFromJsonFile(); // Sélectionne config.json
        
        // Étape 3 : Démarrer le pipeline
        SetupPipeline();
    }

    // Dans MainWindow() ou au démarrage :
    // SetupConfiguration();
}
```

---

## ?? C'est tout !

Vous avez maintenant un système flexible pour charger des types personnalisés et les configurer sans recompiler l'application.

Bon développement ! ??

