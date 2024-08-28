
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour {

public GameObject singleQuad, doubleQuad, diagQuad, pfEntry, pfConfetti;
public Material boxMat, quadWoodMat, quadIronMat, quadBlackMat, quadSpriteMat;
public PhysicMaterial quadPhyMat;
public Sprite soundOn, soundOff;

// Resources
public string[] patternNames;
private string[] levelResources;
public int NLevel => levelResources.Length;

// Constants
public float quadOutSpeed, quadColorExp;
private readonly Color[] quadColors =
{
    Color.red,
    Color.green,
    Color.blue,
    Color.cyan,
    Color.magenta,
    Color.yellow,
    new(1, 0.1f, 0),
    new(1, 0, 0.1f),
    new(0.1f, 1, 0),
    new(0, 1, 0.1f),
    new(0.1f, 0, 1),
    new(0, 0.1f, 1),
};
private readonly Color offTargColor = new(0.02f, 0.02f, 0.02f);
private const int Empty = 0, Wall = 1;
private const int CanMove = 0, HitWall = 1, WoodHitWood = 4, WoodHitIron = 5, IronHitWood = 6, IronHitIron = 7, GetOut = 8;

// Level Info
private int level, xx, yy, modelIdx;
private bool isLShape;

// Game Variables
private int nQuadOut, nextPosCache;
private int[] groundObjs, surfaceObjs, targetObjs;
private readonly List<GameObject> entries = new();
private readonly List<Quad> quads = new();
private readonly Stack<int> history = new();
private Transform box;
private RectTransform entryParent, entryDash;
private AudioController A;
public static bool GLOBAL_INTERACTIVE = true;


#region Enter
private void Start()
{
    levelResources = Resources.Load<TextAsset>($"levels").text.Split("\r\n");
    A = GetComponent<AudioController>();
    A.soundOn = PlayerPrefs.GetInt("soundOn", 1) == 1;
    GameObject.Find("Canvas/sound").GetComponent<UnityEngine.UI.Image>().sprite = A.soundOn ? soundOn : soundOff;
    int gameWinInfo = PlayerPrefs.GetInt("data", 0);
    entryParent = GameObject.Find("Canvas/entries").transform as RectTransform;
    entryDash = GameObject.Find("Canvas/dash").transform as RectTransform;
    for (int i = 0; i < NLevel; i++)
    {
        var entry = Instantiate(pfEntry, entryParent);
        entry.name = $"entry{i}";
        (entry.transform as RectTransform).anchoredPosition = new Vector2(80 * (i % 4) - 120, -80 * (i / 4) + 160);
        entry.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = (i + 1).ToString();
        int j = i;
        entry.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
        {
            A.ClickButton();
            QuitLevel();
            NewGame(j);
        });
        entries.Add(entry);
        if ((gameWinInfo & (1 << i)) != 0)
            entry.GetComponent<UnityEngine.UI.Image>().color = Color.green;
    }
    NewGame(PlayerPrefs.GetInt("level", 0));
}
public void NewGame(int level)
{
    this.level = level;
    PlayerPrefs.SetInt("level", level);
    entryDash.SetParent(entries[level].transform);
    entryDash.anchoredPosition = new Vector2(0, -20);

    var decalScr = new RandomSequence(patternNames.Length);
    var colorScr = new RandomSequence(quadColors.Length);
    int decalIdx = -1, thePattern = 0;
    Color quadColor = default;
    foreach (string s in levelResources[level].Split(' '))
    {
        // Basic Setup
        if(s[0] == '#'){
            xx = s[1] - '0';
            yy = s[2] - '0';
            groundObjs = new int[xx * yy];
            surfaceObjs = new int[xx * yy];
            targetObjs = new int[xx * yy];
            isLShape = s.Length > 3;
            for (int i = 0; i < xx; i++)
            {
                for (int j = 0; j < yy; j++)
                {
                    groundObjs[i + j * xx] = Empty;
                    if (i == 0 || j == 0 || (!isLShape && (i == xx - 1 || j == yy - 1)))
                        groundObjs[i + j * xx] = Wall;
                }
            }
        }

        // Model Setup
        if(s[0] == 'M'){
            modelIdx = int.Parse(s.Substring(1));
            box = Instantiate(Resources.Load<GameObject>($"box{modelIdx}")).transform;
            box.localPosition = 0.2f * Vector3.up;
            Vector4 boxRange = new(xx - 2, yy - 2);
            if (isLShape){
                ++boxRange.x;
                ++boxRange.y;
                box.position += new Vector3(-0.42f, 0, -0.42f);
                box.GetChild(0).transform.localPosition += new Vector3(0.5f, 0, 0.5f);
            }
            box.localScale = (0.4f + 2.0f / boxRange.y) * Vector3.one;

            var copyBoxMat = new Material(this.boxMat);
            copyBoxMat.SetVector("Bound", boxRange);
            var boxObj = box.GetChild(0).gameObject;
            boxObj.GetComponent<MeshRenderer>().material = copyBoxMat;

            var boxCollider = boxObj.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, -0.08f, 0);
            boxCollider.size = new Vector3(boxRange.x, 0.16f, boxRange.y);
            boxCollider.material = quadPhyMat;
        }
    
        // Read Content
        if (s[0] == 'q' || s[0] == 'Q')
        {
            ++thePattern;
            decalIdx = decalScr.Get();
            quadColor = quadColorExp * quadColors[colorScr.Get()];

            bool hasTarget = s.Length >= 5;
            char quadShape = s.Length > 5 ? s[5] : '.';
            var g = Instantiate(quadShape switch
            {
                '/' => diagQuad,
                '\\'=> diagQuad,
                '-' => doubleQuad,
                '|' => doubleQuad,
                _   => singleQuad,
            }, box);
            g.name = patternNames[decalIdx];

            var quad = g.AddComponent<Quad>();
            quad.id = quads.Count;
            quad.pattern = thePattern;
            quad.hasTarget = hasTarget;
            quad.iron = s[0] == 'Q';
            if (quad.iron)
                g.GetComponent<MeshRenderer>().material = quadIronMat;
            quad.atHome = true;
            quad.color = quadColor;
            quad.shape = quadShape;
            quad.initFirstPos = ToNum(s[1]) + ToNum(s[2]) * xx;
            quad.subquads = quadShape switch
            {
                '.' => new Quad.SubQuad[1] {
                    new() { pos = quad.initFirstPos }
                },
                '-' => new Quad.SubQuad[2] {
                    new() { pos = quad.initFirstPos },
                    new() { pos = quad.initFirstPos + 1 }
                },
                '|' => new Quad.SubQuad[2] {
                    new() { pos = quad.initFirstPos },
                    new() { pos = quad.initFirstPos + xx }
                },
                '\\' => new Quad.SubQuad[2] {
                    new() { pos = quad.initFirstPos },
                    new() { pos = quad.initFirstPos - xx + 1 }
                },
                _ => new Quad.SubQuad[2] {
                    new() { pos = quad.initFirstPos },
                    new() { pos = quad.initFirstPos + xx + 1 }
                },
            };
            quad.transform.rotation = quad.InitRotation;
            if (hasTarget)
            {
                int targPos = ToNum(s[3]) + ToNum(s[4]) * xx;
                Sprite sprite = Resources.Load<Sprite>($"targ{g.name}");
                for (int i = 0; i < quad.subquads.Length; i++)
                {
                    // target
                    int thisPos = targPos - quad.initFirstPos + quad.subquads[i].pos;
                    var targ = new GameObject($"targ{g.name}");
                    targ.transform.SetParent(box);
                    targ.transform.localPosition = GetPos(thisPos, 0.001f);
                    targ.transform.rotation = Quaternion.Euler(90, 0, 0);
                    targ.transform.localScale = new Vector3(0.6f, 0.6f, 1);
                    var targSpriteRenderer = targ.AddComponent<SpriteRenderer>();
                    targSpriteRenderer.sprite = sprite;
                    targetObjs[thisPos] = quad.pattern;

                    // decal
                    quad.subquads[i].decalMat = quad.GetComponent<MeshRenderer>().materials[i + 1];

                    // sprite
                    var quadSprite = new GameObject("sprite");
                    quadSprite.transform.SetParent(quad.transform);
                    quadSprite.transform.localPosition = 
                        Quaternion.Inverse(quad.transform.rotation) * new Vector3(
                        quad.subquads[i].pos % xx - quad.initFirstPos % xx,
                        0.201f,
                        quad.subquads[i].pos / xx - quad.initFirstPos / xx);
                    quadSprite.transform.rotation = Quaternion.Euler(90, 0, 0);
                    quadSprite.transform.localScale = new Vector3(0.5f, 0.5f, 1);
                    quad.subquads[i].sprite = quadSprite.AddComponent<SpriteRenderer>();
                    quad.subquads[i].sprite.material = new Material(quadSpriteMat);
                    quad.subquads[i].sprite.color = Color.white;
                    quad.subquads[i].sprite.sprite = sprite;
                }
            }
            quads.Add(quad);
        }
        else if (s[0] == 'w' || s[0] == 'g')
        {
            var obj = s[0] == 'w' ? Wall : Empty;
            if (s.Length == 3)
                groundObjs[ToNum(s[2]) * xx + ToNum(s[1])] = obj;
            else
            {
                int x1 = ToNum(s[1]), x2 = ToNum(s[2]),
                    y1 = ToNum(s[3]), y2 = ToNum(s[4]);
                for (int i = x1; i <= x2; i++)
                    for (int j = y1; j <= y2; j++)
                        groundObjs[j * xx + i] = (groundObjs[j * xx + i] >> 8) << 8 | obj;
            }
        }
    }
    
    Restart();
}
#endregion


#region Moving
public void InputMove(Quad quad, Vector3 v) => InputMove(quad, ParseDirection(v));
public void InputMove(Quad quad, int dir)
{
    if (!GLOBAL_INTERACTIVE || !quad.moveable)
        return;
    SetQuadToArray(quad, false);
    if (GetCode(quad, dir) is CanMove or GetOut)
    {
        history.Push(0);
        StartMove(quad, dir, true);
    }
    else
    {
        SetQuadToArray(quad, true);
    }
}
private int GetSubCode(int pos, int dir)
{
    if (IsGettingOut(pos, dir)) return GetOut;
    nextPosCache = NextPos(pos, dir);
    if ((groundObjs[nextPosCache] & 255) == Wall)
        return HitWall;
    if (surfaceObjs[nextPosCache] != 0 && (surfaceObjs[nextPosCache] >> 8) == 1)
        return quads[surfaceObjs[nextPosCache] & 0xff].iron ? WoodHitIron : WoodHitWood;
    return CanMove;
}
private int GetCode(Quad quad, int dir)
{
    foreach (var subquad in quad.subquads)
    {
        int code = GetSubCode(subquad.pos, dir);
        if (code == GetOut) return GetOut;
        else if (code != CanMove)
        {
            if (quad.iron)
            {
                if (code == WoodHitWood)
                    return IronHitWood;
                if (code == WoodHitIron)
                    return IronHitIron;
            }
            return code;
        }
    }
    return CanMove;
}
private int Simulate(Quad quad, int dir, out int step)
{
    step = 0;
    while (true)
    {
        ++step;
        for (int i = 0; i < quad.subquads.Length; ++i)
            quad.subquads[i].pos = NextPos(quad.subquads[i].pos, dir);
        int code = GetCode(quad, dir);
        if (code != CanMove)
            return code;
    }
}
private void StartMove(Quad quad, int dir, bool powerful)
{
    GLOBAL_INTERACTIVE = false;
    history.Push(quad.id << 16 | quad.FirstPos);
    foreach (var subquad in quad.subquads)
    {
        surfaceObjs[subquad.pos] = 0;
        if (quad.hasTarget)
            SetSubquadColor(subquad, offTargColor);
    }

    var timer = quad.gameObject.AddComponent<Timer>();
    var initPos = quad.transform.localPosition;
    var dirVec = GetDirection(dir);
    if (powerful)
    { 
        int code = Simulate(quad, dir, out int step);
        if (code == GetOut)
        {
            timer.period = step / quadOutSpeed;
            timer.confirmUpdate = false;
            timer.onUpdate = x =>
                quad.transform.localPosition = initPos + x * step * dirVec;
            timer.onStop = () =>
            {
                GLOBAL_INTERACTIVE = true;
                InitQuadOut(quad, dir);
            };
        }
        else
        {
            timer.period = step / quadOutSpeed;
            timer.onUpdate = x =>
                quad.transform.localPosition = initPos + x * step * dirVec;
            timer.onStop = () => EndMove(quad, dir, code, true);
        }
    }
    else // !powerful
    {
        if(IsGettingOut(quad.FirstPos, dir))
        {
            GLOBAL_INTERACTIVE = true;
            InitQuadOut(quad, dir);
        }
        else
        {
            quad.FirstPos = NextPos(quad.FirstPos, dir);
            timer.period = 2.0f / quadOutSpeed;
            timer.onUpdate = x => quad.transform.localPosition = initPos + x * dirVec;
            timer.timeMap = Timer.Accelaration(0, 1, 0.5f);
            timer.onStop = () => EndMove(quad, dir, CanMove, false);
        }
    }
}
private void EndMove(Quad quad, int dir, int code, bool powerful)
{
    GLOBAL_INTERACTIVE = true;
    CheckPos(quad, true);
    foreach (var subquad in quad.subquads)
    {
        surfaceObjs[subquad.pos] = 1 << 8 | quad.id;
    }

    if (powerful)
    {
        if (code is WoodHitIron or IronHitWood or IronHitIron)
        {
            Debug.Assert(quad.subquads.Length == 1);
            if (code == WoodHitIron)
            {
                StartMove(quad, dir ^ 1, false);
                A.QuadIron();
            }
            else
            {
                Quad nextQuad = quads[surfaceObjs[nextPosCache] & 0xff];
                int nextCode = GetCode(nextQuad, dir);
                if (nextCode is CanMove or GetOut)
                {
                    StartMove(nextQuad, dir, code == IronHitIron);
                }
                A.QuadIron();
            }
        }
        else if (code == HitWall){
            if (quad.iron)
                A.QuadIron();
            else
                A.QuadWall();
        }
        else if (code == WoodHitWood)
            A.QuadQuad();
    }

    if (GLOBAL_INTERACTIVE)
    {
        if (quads.All(q => q.atHome))
            GameWin();
    }
}
private void CheckPos(Quad quad, bool playAudio = false)
{
    if (quad.hasTarget)
    {
        quad.atHome = true;
        foreach (var subquad in quad.subquads)
        {
            bool subquadAtHome = targetObjs[subquad.pos] == quad.pattern;
            quad.atHome &= subquadAtHome;
            SetSubquadColor(subquad, subquadAtHome ? quad.color : offTargColor);
        }
        if (playAudio && quad.atHome)
            A.QuadHome();
    }
}
private void SetSubquadColor(Quad.SubQuad subquad, Color color)
{
    subquad.sprite.material.SetColor("_EmissionColor", color);
    subquad.decalMat.SetColor("_EmissionColor", color);
}
private bool IsGettingOut(int pos, int dir) => dir switch
{
    0 => pos % xx == xx - 1,
    1 => pos % xx == 0,
    2 => pos / xx == yy - 1,
    _ => pos / xx == 0
};
private void InitQuadOut(Quad quad, int dir)
{
    quad.moveable = false;
    quad.gameObject.AddComponent<QuadOut>().Init(
        box.localScale.x * quadOutSpeed * GetDirection(dir));
    ++nQuadOut;
}
private void TryRemoveQuadOut(Quad quad)
{
    if (quad.TryGetComponent<QuadOut>(out var quadOut))
    {
        Destroy(quadOut);
        --nQuadOut;
    }
}

#endregion


#region Quiting
public void GameWin()
{
    A.LevelComplete();
    entries[level].GetComponent<UnityEngine.UI.Image>().color = Color.green;
    PlayerPrefs.SetInt("data", PlayerPrefs.GetInt("data") | 1 << (level - 1));
    PlayerPrefs.Save();
    Instantiate(pfConfetti, entryDash);
    (pfConfetti.transform as RectTransform).anchoredPosition = Vector2.zero;
}
public void QuitLevel()
{
    history.Clear();
    quads.Clear();
    Destroy(box.gameObject);
}
#endregion


#region GeneralTools
public static int ToNum(char c) => c >= 'a' ? c - 'a' + 10 : c - '0';
public int NextPos(int pos, int dir)
{
    if (dir == 0) return pos + 1;
    else if (dir == 1) return pos - 1;
    else if (dir == 2) return pos + xx;
    else return pos - xx;
}
public Vector3 GetPos(int x, int z, float y)
{
    return new Vector3(0.5f - xx * 0.5f + x, y, 0.5f - yy * 0.5f + z);
}
public Vector3 GetPos(int pos, float y)
{
    return GetPos(pos % xx, pos / xx, y);
}
public Vector3 GetPosF(float x, float z)
{
    return new Vector3(0.5f - xx * 0.5f + x, 0, 0.5f - yy * 0.5f + z);
}
public Vector3 GetDirection(int dir)
{
    return dir switch
    {
        0 => Vector3.right,
        1 => Vector3.left,
        2 => Vector3.forward,
        _ => Vector3.back
    };
}
public int ParseDirection(Vector3 v)
{
    return (v.z + v.x > 0 ? 0 : 3) ^ (v.z - v.x > 0 ? 2 : 0);
}
#endregion


#region Buttons
public void Restart()
{
    history.Clear();
    if (nQuadOut > 0)
    {
        foreach (var quad in quads)
        {
            if (quad.TryGetComponent<QuadOut>(out var quadOut))
                Destroy(quadOut);
            if (quad.TryGetComponent<Timer>(out var timer))
                Destroy(timer);
        }
        nQuadOut = 0;
    }
    System.Array.Fill(surfaceObjs, 0);
    foreach (Quad quad in quads)
    {
        quad.moveable = true;
        quad.FirstPos = quad.initFirstPos;
        quad.transform.localPosition = GetPos(quad.FirstPos, 0);
        quad.transform.rotation = quad.InitRotation;
        SetQuadToArray(quad, true);
        CheckPos(quad);
    }
    GLOBAL_INTERACTIVE = true;
}
public void Undo()
{
    if (history.Count == 0)
        return;

    List<Quad> quadTogo = new();
    int j = history.Pop();
    while (j != 0)
    {
        var quad = quads[(j >> 16) & 0xf];
        if (surfaceObjs[quad.FirstPos] != 0)
            SetQuadToArray(quad, false);
        quad.moveable = true;
        quad.FirstPos = j & 0xffff;
        quad.transform.localPosition = GetPos(quad.FirstPos, 0);
        quad.transform.rotation = quad.InitRotation;
        CheckPos(quad);
        quadTogo.Add(quad);
        if (nQuadOut > 0)
            TryRemoveQuadOut(quad);
        j = history.Pop();
    }
    foreach (var quad in quadTogo)
        SetQuadToArray(quad, true);
    GLOBAL_INTERACTIVE = true;
}
private void SetQuadToArray(Quad quad, bool addOrRemove)
{
    foreach (var subquad in quad.subquads)
        surfaceObjs[subquad.pos] = addOrRemove ? 1 << 8 | quad.id : 0;
}
#endregion

#region Keys
private void Update()
{
    if (Input.GetKeyUp(KeyCode.Escape))
        ActionEsc();
    else if (Input.GetKeyUp(KeyCode.Z))
        ActionZ();
    else if (Input.GetKeyUp(KeyCode.R))
        ActionR();
    else if (Input.GetKeyUp(KeyCode.N))
        ActionN();
    else if (Input.GetKeyUp(KeyCode.M))
        ActionM();
    else if (Input.GetKeyUp(KeyCode.P))
        ActionP();
}
public void ActionEsc()
{
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
}
public void ActionZ()
{
    A.ClickButton();
    Undo();
}
public void ActionR()
{
    A.ClickButton();
    Restart();
}
public void ActionM(){
    A.ToggleSound();
    GameObject.Find("Canvas/sound").GetComponent<UnityEngine.UI.Image>().sprite = A.soundOn ? soundOn : soundOff;
}
public void ActionN()
{
    A.ClickButton();
    QuitLevel();
    NewGame((level + 1) % NLevel);
}
public void ActionP()
{
    A.ClickButton();
    QuitLevel();
    NewGame((level + NLevel - 1) % NLevel);
}
#endregion

}
