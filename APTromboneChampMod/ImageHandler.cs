using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace APTromboneChampMod;

public static class ImageHandler
{
    // track collections
    
    public static Sprite ArchipelagoCollection;
    public static Sprite ArchipelagoCollectionFiltered;
    public static Sprite ArchipelagoCollectionLocked;

    // connection indicator
    
    public static Texture2D Archipelago;
    public static Texture2D ArchipelagoGrey;

    // hot dog indicator

    public static Texture2D HotDog;
    public static Texture2D HotDogGrey;
    
    // rank indicator

    public static Texture2D RankIndicator;
    public static Texture2D RankIndicatorGrey;
    
    // difficulty indicator

    public static Texture2D DifficultyIndicator;
    public static Texture2D DifficultyIndicatorGrey;
    
    // trap indicators

    public static Texture2D TrapFlipControlsIndicator;
    public static Texture2D TrapSilenceTrackIndicator;
    public static Texture2D TrapSilenceTromboneIndicator;
    public static Texture2D TrapHideNotesIndicator;
    public static Texture2D TrapNoBreathIndicator;
    public static Texture2D TrapWarbleTromboneIndicator;
    public static Texture2D TrapWarpSpeedIndicator;
    
    // death link indicator

    public static Texture2D DeathLinkIndicator;

    public static bool TexturesLoaded = false;
    public static void LoadTextures() {
        FromFile("ap");
        FromFile("ap_grey");
        FromFile("difficulty");
        FromFile("difficulty_grey");
        FromFile("hotdog");
        FromFile("hotdog_grey");
        FromFile("rank");
        FromFile("rank_grey");
        
        FromFile("trap_flip");
        FromFile("trap_silencetrack");
        FromFile("trap_silencetrombone");
        FromFile("trap_hide");
        FromFile("trap_breath");
        FromFile("trap_warble");
        FromFile("trap_warp");
        
        FromFile("deathlink");
        
        FromFileSprite("coll_ap");
        FromFileSprite("coll_ap_filter");
        FromFileSprite("coll_ap_locked");
    }

    private static void FromFile(string name) {
        string path = Path.Combine(Path.GetDirectoryName(ArchipelagoPlugin.Instance.Info.Location), $"{name}.png");
        ArchipelagoPlugin.Logger.LogInfo($"Loading texture from {path}");
        UnityWebRequest req = UnityWebRequestTexture.GetTexture(path);
        UnityWebRequestAsyncOperation op = req.SendWebRequest();
        bool done = false;
        op.completed += _ => {
            done = true;
            SetTexture(req.downloadHandler.data, name);
        };
        if (op.isDone && !done) SetTexture(req.downloadHandler.data, name);
    }

    private static void SetTexture(byte[] data, string name) {
        Texture2D texture = new(1, 1);
        texture.LoadImage(data);
        switch (name) {
            case "ap":
                Archipelago = texture;
                break;
            case "ap_grey":
                ArchipelagoGrey = texture;
                break;
            case "difficulty":
                DifficultyIndicator = texture;
                break;
            case "difficulty_grey":
                DifficultyIndicatorGrey = texture;
                break;
            case "hotdog":
                HotDog = texture;
                break;
            case "hotdog_grey":
                HotDogGrey = texture;
                break;
            case "rank":
                RankIndicator = texture;
                break;
            case "rank_grey":
                RankIndicatorGrey = texture;
                break;
            case "trap_flip":
                TrapFlipControlsIndicator = texture;
                break;
            case "trap_silencetrack":
                TrapSilenceTrackIndicator = texture;
                break;
            case "trap_silencetrombone":
                TrapSilenceTromboneIndicator = texture;
                break;
            case "trap_hide":
                TrapHideNotesIndicator = texture;
                break;
            case "trap_breath":
                TrapNoBreathIndicator = texture;
                break;
            case "trap_warble":
                TrapWarbleTromboneIndicator = texture;
                break;
            case "trap_warp":
                TrapWarpSpeedIndicator = texture;
                break;
            case "deathlink":
                DeathLinkIndicator = texture;
                break;
            default:
                ArchipelagoPlugin.Logger.LogWarning($"Texture loaded but unknown name: {name}");
                break;
        }
        if (
            Archipelago && ArchipelagoGrey &&
            DifficultyIndicator && DifficultyIndicatorGrey &&
            HotDog && HotDogGrey &&
            RankIndicator && RankIndicatorGrey &&
            ArchipelagoCollection && ArchipelagoCollectionFiltered &&
            ArchipelagoCollectionLocked && TrapFlipControlsIndicator &&
            TrapSilenceTrackIndicator && TrapSilenceTromboneIndicator &&
            TrapHideNotesIndicator && TrapNoBreathIndicator &&
            TrapWarbleTromboneIndicator && TrapWarpSpeedIndicator &&
            DeathLinkIndicator
        ) TexturesLoaded = true;
    }

    private static void FromFileSprite(string name) {
        string path = Path.Combine(Path.GetDirectoryName(ArchipelagoPlugin.Instance.Info.Location), $"{name}.png");
        ArchipelagoPlugin.Logger.LogInfo($"Loading texture from {path}");
        UnityWebRequest               req  = UnityWebRequestTexture.GetTexture(path);
        UnityWebRequestAsyncOperation op   = req.SendWebRequest();
        bool                          done = false;
        op.completed += _ => {
            done = true;
            SetSprite(req.downloadHandler.data, name);
        };
        if (op.isDone && !done) SetSprite(req.downloadHandler.data, name);
    }

    private static void SetSprite(byte[] data, string name) {
        Texture2D texture = new(1, 1);
        texture.LoadImage(data);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        switch (name) {
            case "coll_ap":
                ArchipelagoCollection = sprite;
                break;
            case "coll_ap_filter":
                ArchipelagoCollectionFiltered = sprite;
                break;
            case "coll_ap_locked":
                ArchipelagoCollectionLocked = sprite;
                break;
            default:
                ArchipelagoPlugin.Logger.LogWarning($"Sprite loaded but unknown name: {name}");
                break;
        }
        if (
            Archipelago && ArchipelagoGrey &&
            DifficultyIndicator && DifficultyIndicatorGrey &&
            HotDog && HotDogGrey &&
            RankIndicator && RankIndicatorGrey &&
            ArchipelagoCollection && ArchipelagoCollectionFiltered && ArchipelagoCollectionLocked
        ) TexturesLoaded = true;
    }
}