# ModLoader - Comment le Chargement Automatique Fonctionne

Ce document explique comment ModLoader se charge automatiquement au démarrage du jeu.

## 🚀 Chargement Automatique

### Étape 1 : Unity Reconnaît la DLL

Lorsque vous ajoutez `ModLoader.dll` à `ScriptingAssemblies.json` :

```json
{
    "names": [
        "Scripts.dll",
        "ModLoader.dll"  ← Votre DLL ici
    ]
}
```

Unity sait qu'il doit charger cette DLL au démarrage.

### Étape 2 : Unity Scanne les Attributs

Au démarrage, Unity scanne **toutes les DLLs chargées** pour trouver les méthodes avec l'attribut `[RuntimeInitializeOnLoadMethod]`.

### Étape 3 : Unity Appelle Automatiquement `Initialize()`

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize()
{
    // Cette méthode est appelée AUTOMATIQUEMENT par Unity
    // Pas besoin de l'appeler manuellement !
}
```

## 📋 Ordre de Chargement

```
1. Unity démarre
   ↓
2. Unity charge toutes les DLLs de ScriptingAssemblies.json
   ↓
3. Unity scanne pour [RuntimeInitializeOnLoadMethod]
   ↓
4. Unity appelle ModLoader.Core.Initialize()
   ↓
5. ModLoader crée le ModManager
   ↓
6. ModManager scanne le dossier Mods/
   ↓
7. ModManager charge tous les mods (DLLs)
   ↓
8. Chaque mod.OnLoad() est appelé
   ↓
9. Les mods appliquent leurs patches Harmony
   ↓
10. Le jeu continue normalement avec les mods actifs
```

## 🎯 Types de RuntimeInitializeLoadType

```csharp
// Avant le chargement de la première scène
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]

// Avant le Awake() des objets
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]

// Avant le Start() des objets
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]

// Après le chargement de la scène (RECOMMANDÉ pour ModLoader)
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

// Sous-système de scène
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```

**Pour ModLoader, on utilise `AfterSceneLoad`** car :
- Les systèmes Unity sont initialisés
- Les managers du jeu existent
- On peut accéder à l'UI
- Les mods peuvent patcher les systèmes déjà chargés

## 🔍 Vérifier que ça Fonctionne

### Dans les Logs Unity

Vous devriez voir ces messages :

```
[ModLoader] ModLoader v1.0.0 Starting...
[ModLoader] Loaded automatically via RuntimeInitializeOnLoadMethod
[ModLoader] Scanning for mods in: C:/Path/To/Game/Mods
[ModLoader] Found X DLL file(s)
[ModLoader] ✓ Loaded mod: ModName v1.0.0 by Author
[ModLoader] ModLoader initialized successfully! Loaded X mod(s)
```

### Si Rien ne S'affiche

1. **Vérifiez ScriptingAssemblies.json**
   - Le fichier est dans `BrokeProtocol_Data/`
   - `ModLoader.dll` est dans la liste `names`
   - Pas d'erreur de syntaxe JSON

2. **Vérifiez l'emplacement de la DLL**
   - `ModLoader.dll` doit être dans `BrokeProtocol_Data/Managed/`
   - Pas dans `Mods/` (sinon Unity ne le charge pas)

3. **Vérifiez les dépendances**
   - `0Harmony.dll` est présent
   - Toutes les DLLs Unity nécessaires sont présentes

4. **Vérifiez les logs Unity**
   - Fichier log : `BrokeProtocol_Data/output_log.txt`
   - Ou dans Player.log (voir section ci-dessous)

## 📁 Emplacements des Fichiers

```
BrokeProtocol/
├── BrokeProtocol.exe
├── BrokeProtocol_Data/
│   ├── ScriptingAssemblies.json    ← Ajouter ModLoader.dll ici
│   ├── output_log.txt              ← Logs Unity (parfois)
│   └── Managed/
│       ├── ModLoader.dll           ← Votre DLL ICI
│       ├── !_0Harmony.dll          ← Dépendance
│       ├── Scripts.dll
│       └── ... (autres DLLs Unity)
└── Mods/                           ← Les mods des utilisateurs
    ├── MonMod.dll
    └── Logs/
        └── MonMod.log
```

## 🛠️ Debugging

### Ajouter des Logs de Debug

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize()
{
    Debug.Log("[ModLoader] Initialize() was called!");
    Debug.Log($"[ModLoader] Current Time: {DateTime.Now}");
    Debug.Log($"[ModLoader] Data Path: {Application.dataPath}");
    
    // ... reste du code
}
```

### Vérifier dans Player.log

**Windows :**
```
C:\Users\[Username]\AppData\LocalLow\[Company]\[Game]\Player.log
```

**Ou dans le jeu :**
```
%USERPROFILE%\AppData\LocalLow\Broke Protocol\Broke Protocol\Player.log
```

## ⚠️ Erreurs Communes

### 1. "ModLoader not found in ScriptingAssemblies.json"

**Solution :** Ajoutez-le manuellement :

```json
{
    "names": [
        ...,
        "ModLoader.dll"
    ],
    "types": [
        ...,
        16
    ]
}
```

**Note :** Le nombre de `types` doit correspondre au nombre de `names` !

### 2. "Method not found: ModLoader.Core.Initialize"

**Cause :** La méthode n'est pas static ou l'attribut est mal placé.

**Solution :** Vérifiez :
```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
public static void Initialize() // DOIT être static
```

### 3. "ModLoader.dll is missing dependencies"

**Cause :** Harmony ou autres DLLs manquantes.

**Solution :** Vérifiez que toutes les références sont présentes dans `Managed/`

### 4. "Initialize() called multiple times"

**Cause :** Plusieurs instances ou appels multiples.

**Solution :** Le code a déjà une vérification :
```csharp
if (initialized)
{
    Debug.LogWarning("[ModLoader] Already initialized, skipping...");
    return;
}
```

## 🎓 Alternatives au Chargement Automatique

Si `[RuntimeInitializeOnLoadMethod]` ne fonctionne pas, voici d'autres méthodes :

### Méthode 2 : Static Constructor

```csharp
public class Core
{
    static Core()
    {
        // S'exécute la première fois que la classe est référencée
        Initialize();
    }
}
```

**Problème :** Ne s'exécute que si quelque chose référence la classe.

### Méthode 3 : MonoBehaviour avec GameObject

```csharp
public class ModLoaderBehaviour : MonoBehaviour
{
    void Awake()
    {
        Core.Initialize();
        DontDestroyOnLoad(gameObject);
    }
}
```

**Problème :** Nécessite un GameObject dans la scène.

### Méthode 4 : Patch Harmony Bootstrap

Créer une DLL séparée qui patch le démarrage du jeu :

```csharp
[HarmonyPatch(typeof(SceneManager), "Awake")]
class Bootstrap
{
    static void Postfix()
    {
        ModLoader.Core.Initialize();
    }
}
```

## ✅ Recommandation Finale

**Utilisez `[RuntimeInitializeOnLoadMethod]`** - C'est :
- ✅ Automatique
- ✅ Fiable
- ✅ Standard Unity
- ✅ Ne nécessite pas de modification du jeu
- ✅ Fonctionne avec juste ScriptingAssemblies.json

## 📞 Support

Si le chargement automatique ne fonctionne toujours pas :

1. Vérifiez `Player.log` pour les erreurs
2. Ajoutez des `Debug.Log()` pour tracer l'exécution
3. Vérifiez que Unity charge bien votre DLL
4. Testez avec un mod simple d'abord

---

**Le ModLoader est maintenant complètement automatique !** 🎉

Ajoutez simplement `ModLoader.dll` dans `ScriptingAssemblies.json` et dans `Managed/`, et il se chargera automatiquement au démarrage du jeu.

