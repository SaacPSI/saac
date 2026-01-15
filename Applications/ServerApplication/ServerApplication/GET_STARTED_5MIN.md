# ?? DÉMARRAGE IMMÉDIAT - 5 Minutes

## ?? C'est parti !

Suivez ces étapes exactement pour voir le système en action en 5 minutes.

---

## Étape 1 : Créer une DLL Simple (1 min)

### Ouvrez Visual Studio et créez un **Class Library (.NET Framework 4.8)** :

```
Fichier ? Nouveau Projet ? Class Library ? .NET Framework 4.8
Nom : MyTestData
```

### Remplacez le contenu par :

```csharp
using System;

namespace MyTestData
{
    public class TestData
    {
        public string Message { get; set; }
        public float Value { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"Message: {Message}, Value: {Value}";
        }
    }
}
```

### Compilez :
```
Générer ? Générer la solution
```

**Localisation du .dll généré** :
```
MyTestData\bin\Release\MyTestData.dll
```

---

## Étape 2 : Charger la DLL (1 min)

### Dans votre application SAAC, appelez :

```csharp
LoadAssemblyFromFile();
```

**Résultat attendu** :
- Une boîte de dialogue s'ouvre
- Sélectionnez `MyTestData.dll`
- Logs affichent :
  ```
  ? DLL chargée : C:\...\MyTestData.dll
  ? Template JSON généré : C:\...\config_template.json
  ```

### Localisez le fichier généré
```
Cherchez config_template.json dans le même dossier que MyTestData.dll
```

---

## Étape 3 : Modifier la Configuration (1 min)

### Ouvrez le fichier `config_template.json` généré

**Contenu généré** :
```json
[
  {
    "topic": "testdata",
    "type": "TestData",
    "class": "MyTestData.TestData, MyTestData",
    "transformer": null
  }
]
```

### Modifiez-le pour ajouter deux topics :

```json
[
  {
    "topic": "test_message",
    "type": "TestData",
    "class": "MyTestData.TestData, MyTestData",
    "transformer": null
  },
  {
    "topic": "test_value",
    "type": "TestData",
    "class": "MyTestData.TestData, MyTestData",
    "transformer": null
  }
]
```

### Renommez en `config.json` et sauvegardez

---

## Étape 4 : Charger la Configuration (1 min)

### Dans votre application SAAC, appelez :

```csharp
LoadConfigurationFromJsonFile();
```

**Résultat attendu** :
- Une boîte de dialogue s'ouvre
- Sélectionnez `config.json`
- Logs affichent :
  ```
  ? Configuration JSON chargée : C:\...\config.json
    Topics configurés : test_message, test_value
  ```

---

## Étape 5 : Vérifier (1 min)

### Vérifiez dans le code que les topics sont configurés :

```csharp
// Ajouter dans n'importe quel event handler
foreach (var topic in configuration.TopicsTypes.Keys)
{
    AddLog($"? Topic chargé : {topic}");
}
```

**Résultat attendu** :
```
? Topic chargé : test_message
? Topic chargé : test_value
```

---

## ?? C'est tout !

Vous avez maintenant :
- ? Créé une DLL avec des classes
- ? Chargé la DLL dynamiquement
- ? Créé une configuration JSON
- ? Chargé la configuration
- ? Intégré avec le pipeline

**Temps total : 5 minutes ! ??**

---

## ?? Prochaines Étapes

### Option 1 : Ajouter des Boutons (5 min)

Modifiez `MainWindow.xaml` pour ajouter :

```xaml
<Button Content="Load DLL" Click="BtnLoadAssembly_Click" />
<Button Content="Load Config" Click="BtnLoadTopicsJson_Click" />
```

Les event handlers sont déjà implémentés ! 

### Option 2 : Apprendre à Fond (30 min)

Lisez :
1. QUICKSTART.md
2. CONFIGURATION_LOADER_README.md
3. ARCHITECTURE.md

### Option 3 : Utiliser en Production (Maintenant !)

Vous pouvez déjà utiliser le système en production.

---

## ?? Points Clés à Retenir

1. **Ordre** : DLL d'abord, puis JSON
2. **Noms** : Utiliser le nom complet (Namespace.Class)
3. **Template** : Généré automatiquement, ne pas créer manuellement
4. **Logs** : Consultez-les, ils disent tout
5. **JSON** : Format doit être exact

---

## ?? Si Ça ne Marche Pas

### Erreur : "Type introuvable"
? Vérifier le format : "Namespace.ClassName, AssemblyName"

### Erreur : "DLL non trouvée"
? Utiliser la boîte de dialogue, pas taper le chemin

### Erreur : "Veuillez charger DLL"
? Charger la DLL **avant** le JSON

### Erreur : "JSON invalide"
? Générer un nouveau template et modifier

---

## ?? Exemples Supplémentaires

### Exemple 1 : Types System Simples

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

### Exemple 2 : Plusieurs Types

```json
[
  {
    "topic": "name",
    "type": "string",
    "class": "System.String, System.Private.CoreLib",
    "transformer": null
  },
  {
    "topic": "age",
    "type": "int",
    "class": "System.Int32, System.Private.CoreLib",
    "transformer": null
  }
]
```

---

## ?? Ressources

- **QUICKSTART.md** : Guide détaillé
- **config_example.json** : Autre exemple
- **EXAMPLE_DATA_CLASSES.cs** : Plus de classes
- **HELP_INLINE.txt** : FAQ complet

---

## ? Vous Êtes Prêt !

Vous maîtriserez le système après ces 5 minutes.

**Amusez-vous bien ! ??**

