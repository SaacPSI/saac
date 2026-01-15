# Architecture - Configuration Loader

## ?? Vue d'Ensemble

```
???????????????????????????????????????????????????????????????
?                    MainWindow.xaml.cs                        ?
?                  (Interface Utilisateur)                     ?
?                                                              ?
?  • LoadAssemblyFromFile()                                    ?
?  • LoadConfigurationFromJsonFile()                           ?
?                                                              ?
???????????????????????????????????????????????????????????????
                   ?
                   ?
???????????????????????????????????????????????????????????????
?            ServerApplication/Helpers/                        ?
?           ConfigurationLoader.cs                             ?
?                                                              ?
?  • LoadAssemblyTypes()      ? Charge une DLL               ?
?  • LoadConfigurationFromJson() ? Charge JSON                ?
?  • ExportConfigurationTemplate() ? Génère template          ?
?  • ResolveType()            ? Résout les types              ?
?                                                              ?
???????????????????????????????????????????????????????????????
       ?                                          ?
       ?                                          ?
  [Your DLL]                               [config.json]
  MyDataClasses.dll                        Topics config
  ? SensorData                             ? sensor_temp
  ? AudioFrame                             ? audio_stream
  ? VideoFrame                             ? video_stream
  ?? Types découverts                      ?? Références types
```

---

## ?? Flux de Données

### 1. Chargement de la DLL

```
User clicks "Load DLL"
         ?
         ?
[OpenFileDialog]
         ?
         ?
LoadAssemblyFromFile()
         ?
         ?
Assembly.LoadFrom(dllPath)
         ?
         ?? Découvre tous les types
         ?? Crée Assembly object
         ?? Stocke dans customAssembly
         ?
         ?
ExportConfigurationTemplate()
         ?
         ?? Génère config_template.json
```

### 2. Chargement de la Configuration

```
User clicks "Load JSON"
         ?
         ?
[OpenFileDialog]
         ?
         ?
LoadConfigurationFromJsonFile()
         ?
         ?
File.ReadAllText(jsonPath)
         ?
         ?
JsonConvert.DeserializeObject<List<TopicConfigEntry>>()
         ?
         ?
For each entry:
  ? ResolveType(entry.Class)
  ?   ?? Type.GetType() ? Try built-in types
  ?   ?? customAssembly.GetType() ? Try custom types
  ?
  ?? Add to config.TopicsTypes[topic] = type
  ?? Add to config.Transformers[topic] = transformer
         ?
         ?
? Configuration chargée !
```

---

## ?? Classes Principales

### `TopicConfigEntry`
Représentation d'un topic dans le JSON

```csharp
public class TopicConfigEntry
{
    public string Topic { get; set; }        // ex: "sensor_temp"
    public string Type { get; set; }         // ex: "SensorData"
    public string Class { get; set; }        // ex: "MyDataClasses.SensorData, MyDataClasses"
    public string? Transformer { get; set; } // ex: "MyDataClasses.SensorTransformer, MyDataClasses"
}
```

### Intégration avec `RendezVousPipelineConfiguration`

```csharp
configuration.TopicsTypes[topic]  // Dictionary<string, Type>
configuration.Transformers[topic] // Dictionary<string, Type>
configuration.TypesSerializers[type] // Dictionary<Type, IPsiFormat>
```

---

## ?? Dépendances

```
MainWindow.xaml.cs
    ?
    ?? ConfigurationLoader.cs
            ?
            ?? System.IO              (File I/O)
            ?? System.Reflection      (Assembly Loading)
            ?? Newtonsoft.Json        (JSON Parsing)
            ?? SAAC.PipelineServices  (RendezVousPipelineConfiguration)
                    ?
                    ?? Microsoft.Psi   (Pipeline)
                    ?? Custom Types    (From User DLL)
```

---

## ?? Mécanisme de Résolution de Types

### Étape 1 : Types Built-in (System)

```csharp
Type type = Type.GetType("System.String, System.Private.CoreLib");
// ? Trouvé dans les assemblies système
```

### Étape 2 : Types Personnalisés

```csharp
Type type = customAssembly.GetType("MyDataClasses.SensorData");
// ? Trouvé dans la DLL chargée
```

### Étape 3 : Erreur

```csharp
if (type == null)
    throw new TypeLoadException($"Type introuvable : {typeFullName}");
// ? Erreur affichée dans les logs
```

---

## ?? Exemple d'Exécution Complète

```
[1] User clique "Load DLL"
    ? Dialogue sélection : C:\MyDataClasses.dll
    ? customAssembly = Assembly.LoadFrom(...)
    ? Types découverts : SensorData, AudioFrame, VideoFrame
    ? Template généré : C:\config_template.json
    ? LOG: ? DLL chargée
    ? LOG: ? Template JSON généré

[2] User modifie config.json

[3] User clique "Load JSON"
    ? Dialogue sélection : C:\config.json
    ? Parse JSON
    
    Pour chaque entrée :
    ?? topic = "sensor_temp"
    ?? class = "MyDataClasses.SensorData, MyDataClasses"
    ?? ResolveType("MyDataClasses.SensorData, MyDataClasses")
    ?   ?? customAssembly.GetType("MyDataClasses.SensorData")
    ?   ?? ? Type found
    ?? config.TopicsTypes["sensor_temp"] = SensorData (type)
    ?
    ?? Repeat pour tous les topics
    
    ? LOG: ? Configuration JSON chargée
    ? LOG: Topics configurés : sensor_temp, audio_stream, video_stream

[4] Pipeline démarre avec les types chargés
    ? Streams typés correctement
    ? Transformers appliqués si spécifiés
```

---

## ?? Sécurité & Validations

### Validations implémentées :

1. **Fichier existe** ? FileNotFoundException
2. **Assembly valide** ? InvalidOperationException
3. **JSON valide** ? JsonException
4. **Types existent** ? TypeLoadException
5. **DLL avant JSON** ? Vérification dans LoadConfigurationFromJsonFile()

---

## ?? Points Clés

| Aspect | Détail |
|--------|--------|
| **Ordre** | DLL ? JSON (strict) |
| **Format JSON** | Array of TopicConfigEntry |
| **Types** | Nom qualifié complet requis |
| **Transformers** | Optionnels (peuvent être null) |
| **Templates** | Auto-générés pour faciliter |
| **Logs** | Tous les événements affichés |
| **Erreurs** | Captures et affichées |

---

## ?? Liveries (NuGet)

- **Newtonsoft.Json** : Sérialisation JSON
  - ? Compatible .NET Framework 4.8
  - Probablement déjà dans les dépendances

---

## ?? Prochaines Améliorations Possibles

1. **Cache** : Mettre en cache les assemblies chargées
2. **Validation** : Valider les types avant de charger
3. **UI** : Ajouter des boutons à l'interface
4. **Hot-reload** : Recharger sans redémarrer
5. **Plugins** : Système de plugins dynamiques
6. **Types génériques** : Support List<T>, Dictionary<K,V>
7. **Filtering** : Filtrer les types par interface/attribut

