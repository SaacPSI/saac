# Configuration Loader - Guide d'utilisation

## Fonctionnalités

Cet outil permet de :

1. **Charger une DLL** contenant vos classes de données personnalisées
2. **Charger une configuration JSON** qui mappe les topics aux types de données

## Utilisation

### Étape 1 : Charger une DLL

```csharp
// Dans MainWindow, appeler :
LoadAssemblyFromFile();
```

Cela ouvrira un dialogue pour sélectionner une DLL. Une fois chargée :
- Les types disponibles sont découverts automatiquement
- Un fichier template `config_template.json` est généré dans le même répertoire

### Étape 2 : Configurer les Topics

Modifiez le fichier JSON généré ou utilisez l'exemple fourni.

#### Format du JSON

```json
[
  {
    "topic": "nom_du_topic",
    "type": "nom_du_type",
    "class": "Namespace.ClassName, AssemblyName",
    "transformer": "Namespace.TransformerClass, AssemblyName"
  }
]
```

**Champs obligatoires** :
- `topic` : identifiant unique du flux de données
- `type` : nom du type (informatif)
- `class` : nom qualifié complet du type (format : "Namespace.Class, Assembly")

**Champs optionnels** :
- `transformer` : classe transformer personnalisée (format : "Namespace.Class, Assembly")

### Étape 3 : Charger la Configuration

```csharp
// Dans MainWindow, appeler :
LoadConfigurationFromJsonFile();
```

Cela ouvrira un dialogue pour sélectionner un fichier JSON. Une fois chargé :
- Les topics sont enregistrés dans `configuration.TopicsTypes`
- Les transformers (si spécifiés) sont enregistrés dans `configuration.Transformers`
- Les logs affichent les topics configurés

## Exemple Complet

### 1. DLL personnalisée (MyDataClasses.dll)

```csharp
namespace MyDataClasses
{
    public class SensorData
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
    }

    public class AudioFrame
    {
        public byte[] Data { get; set; }
        public int SampleRate { get; set; }
    }
}
```

### 2. Configuration JSON (config.json)

```json
[
  {
    "topic": "sensor",
    "type": "SensorData",
    "class": "MyDataClasses.SensorData, MyDataClasses",
    "transformer": null
  },
  {
    "topic": "audio",
    "type": "AudioFrame",
    "class": "MyDataClasses.AudioFrame, MyDataClasses",
    "transformer": "MyDataClasses.AudioTransformer, MyDataClasses"
  }
]
```

### 3. Code d'utilisation

```csharp
public MainWindow()
{
    // ...
    LoadAssemblyFromFile();     // Charger MyDataClasses.dll
    LoadConfigurationFromJsonFile(); // Charger config.json
    // ...
}
```

## Intégration avec l'UI

Pour ajouter des boutons dans l'interface :

```xaml
<Button Content="Charger DLL" Click="BtnLoadAssembly_Click" />
<Button Content="Charger Configuration" Click="BtnLoadConfiguration_Click" />
```

```csharp
private void BtnLoadAssembly_Click(object sender, RoutedEventArgs e)
{
    LoadAssemblyFromFile();
}

private void BtnLoadConfiguration_Click(object sender, RoutedEventArgs e)
{
    LoadConfigurationFromJsonFile();
}
```

## Gestion des erreurs

L'outil affiche les erreurs dans le log :
- ? Succès
- ? Erreur
- ? Avertissement

Consultez les messages pour diagnostiquer les problèmes.

## Restriction importante

?? **La DLL DOIT être chargée avant le JSON** pour que les types soient résolus correctement.

