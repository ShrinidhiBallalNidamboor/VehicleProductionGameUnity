using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DialogManager : MonoBehaviour
{
    [SerializeField] private GameObject _dialogBox;
    [SerializeField] private GameObject _board;
    [SerializeField] private GameObject _menuUI;
    [SerializeField] private GameObject _orderUI;
    [SerializeField] private GameObject _inventoryUI;
    [SerializeField] private GameObject _computerUI;
    [SerializeField] private GameObject _menu;
    [SerializeField] private GameObject _order;
    [SerializeField] private GameObject _inventorylist;
    [SerializeField] private GameObject _computer;
    [SerializeField] private GameObject _yellow;
    [SerializeField] private GameObject _green;
    [SerializeField] private GameObject _incrate;
    [SerializeField] private GameObject _outcrate;
    
    [SerializeField] private float _dialogSpeed;
    [SerializeField] private AudioClip _dialogSFX;

    //Dialog information
    [SerializeField] private Dialog _dialog;
    private Dialog _currentDialog;
    private int _currentDialogLine;
    private Text _dialogText;
    private Tween _dialogTextTween;
    private Action _onCurrentDialogFinish;

    //Menu control
    private int _state = 0;
    private float scroll = 1.035f;
    private int childCount = 0;
    private int index = 0;
    private int pointer = 0;

    public event Action OnDialogStart;
    public event Action OnDialogFinish;

    public static DialogManager SharedInstance;
    private bool IsDialogLineBeingWritten => _dialogTextTween != null && _dialogTextTween.IsPlaying();

    //File control
    private string filemessenger = "../Database/Messenger/Manager2.txt";
    private int messengerlength = 0;
    private string filewarehouse = "../Database/Warehouse/Warehouse2.txt";

    //Manager specific info
    private int numberofproducts = 3;
    private int user = 2;

    private void Awake() {
        if (SharedInstance == null)
        {
            SharedInstance = this;
        }
        else
        {
            print("There's more than one DialogManager instance!");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start() {
        _dialogText = _dialogBox.GetComponentInChildren<Text>();
    }

    private async void Update() {
        if (!File.Exists(filemessenger))
        {
            Console.WriteLine("The file does not exist.");
            return;
        }
        try
        {
            int length = 0;
            string lastline = null;
            using (StreamReader reader = new StreamReader(filemessenger))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    length += 1;
                    lastline = line;
                }
                if (length != messengerlength)
                {
                    messengerlength = length;
                    if (lastline.Substring(0, 6) == "Target")
                    {
                        _dialog.Lines[0] = lastline;
                    }
                    else{
                        await MoveCrate(_incrate, 6, 1);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred while reading the file:");
            Console.WriteLine(e.Message);
        }
    }

    public async void HandleUpdate()
    {
        if (Input.GetButtonDown("Submit"))
        {
            if (_state == 1)
            {
                if (IsDialogLineBeingWritten)
                {
                    _dialogTextTween.Complete();
                    return;
                }

                if (++_currentDialogLine < _currentDialog.Lines.Count)
                {
                    ShowCurrentDialogLine();
                }
                else
                {
                    EndDialog();
                }
            }
            else if (_state == 2)
            {
                EndMenu(_state);
                if (pointer == 0)
                {
                    InventoryUpdate();
                    ShowMenu(3);
                }
                else if (pointer == 1)
                {
                    ShowMenu(4);
                }
            }
            else if (_state == 4)
            {
                if (pointer == childCount-1)
                {
                    ReceiveOrder();
                    EndMenu(_state);
                    return;
                }
                GameObject component = Match(4);
                ToggleColor(component, pointer, "red");
                _state = 5;
            }
            else if (_state == 5)
            {
                GameObject component = Match(4);
                ToggleColor(component, pointer, "blue");
                _state = 4;
            }
            else if (_state == 6)
            {
                if (pointer == 0)
                {
                    GameObject component = Match(6);
                    _state = 7;
                    ToggleColor(component, pointer, "red");
                    await ToggleBlub();
                    RunPython("python3", "../Managers/Manager.py", user, "", 3);
                    ToggleColor(component, pointer, "blue");
                    _state = 6;
                }
                else if (pointer == 1)
                {
                    GameObject component = Match(6);
                    _state = 7;
                    ToggleColor(component, pointer, "red");
                    await MoveCrate(_outcrate, 6, -1);
                    RunPython("python3", "../Managers/Manager.py", user, "", 2);
                    ToggleColor(component, pointer, "blue");
                    _state = 6;
                }
                else
                {
                    EndMenu(_state);
                }
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == 1)
            {
                if (IsDialogLineBeingWritten)
                {
                    _dialogTextTween.Complete();
                }
                EndDialog();
            }
            else if (_state != 5 && _state != 7)
            {
                EndMenu(_state);
            }
        }
        else if (Input.GetButtonDown("Up"))
        {
            if (_state == 2 || _state == 3 || _state == 4 || _state == 6)
            {
                ToggleColor(Match(_state), pointer, "black");
                Scrolling(_state, -1, 0, 0);
                ToggleColor(Match(_state), pointer, "blue");
            }
            else if (_state == 5)
            {
                CountChange(Match(4), pointer, 1);
            }
        }
        else if (Input.GetButtonDown("Down"))
        {
            if (_state == 2 || _state == 3 || _state == 4 || _state == 6)
            {
                ToggleColor(Match(_state), pointer, "black");
                Scrolling(_state, 1, 3, childCount-1);
                ToggleColor(Match(_state), pointer, "blue");
            }
            else if (_state == 5)
            {
                CountChange(Match(4), pointer, -1);
            }
        }
    }

    public void ReceiveOrder()
    {
        string[] value = new string[childCount-1+numberofproducts];
        for(int i=0;i<childCount-1;i=i+1)
        {
            Text text =_order.transform.GetChild(i).GetChild(1).transform.GetComponent<Text>();
            value[i] = text.text;
            text.text = "0";
        }
        for(int i=0;i<numberofproducts;i=i+1)
        {
            value[i+childCount-1]="0";
        }
        string order = String.Join(",", value);
        RunPython("python3", "../Managers/Manager.py", user, order, 1);
    }

    public void RunPython(string pythonExe, string scriptPath, int arg1, string arg2, int arg3)
    {
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = pythonExe;
        start.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\"", scriptPath, arg1, arg2, arg3);
        start.UseShellExecute = false;
        start.CreateNoWindow = true;

        // Start the process
        Process process = Process.Start(start);
    }

    public void InventoryUpdate()
    {
        _inventoryUI.SetActive(true);
        if (!File.Exists(filewarehouse))
        {
            Console.WriteLine("The file does not exist.");
            return;
        }
        try
        {
            using (StreamReader reader = new StreamReader(filewarehouse))
            {
                string line;
                int pointer = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    string count = line.Split('-')[1].Split(',')[0];
                    Console.WriteLine(count);
                    CountUpdate(Match(3), pointer, count);
                    pointer += 1;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred while reading the file:");
            Console.WriteLine(e.Message);
        }
    }

    public async Task ToggleBlub()
    {
        for(int i=0;i<4;i=i+1)
        {
            _green.SetActive(true);
            _yellow.SetActive(false);
            await Task.Delay(500);
            _green.SetActive(false);
            _yellow.SetActive(true);
            await Task.Delay(500);
        }
    }

    public async Task MoveCrate(GameObject component, int steps, float amount)
    {
        component.SetActive(true);
        for(int i=0;i<steps;i=i+1)
        {
            await Task.Delay(500);
            Vector3 coordinate=component.transform.position;
            coordinate.y=coordinate.y+amount;
            component.transform.position=coordinate;
        }
        component.SetActive(false);
        for(int i=0;i<steps;i=i+1)
        {
            Vector3 coordinate=component.transform.position;
            coordinate.y=coordinate.y-amount;
            component.transform.position=coordinate;
        }
    }

    public void Scrolling(int type, int indexing, int top, int end)
    {
        if (index == top)
        {
            GameObject component=Match(type);
            Vector3 coordinate=component.transform.position;
            coordinate.y=coordinate.y+scroll*indexing;
            component.transform.position=coordinate;
        }
        else
        {
            index+=indexing;
        }
        if (pointer != end)
        {
            pointer += indexing;
        }
    }

    public void CountChange(GameObject component, int pointer, int indexing)
    {
        Text text = component.transform.GetChild(pointer).GetChild(1).transform.GetComponent<Text>();
        int number = int.Parse(text.text);
        number += indexing;
        if (number < 0){
            number = 0;
        }
        text.text = $"{number}";
    }
    
    public void CountUpdate(GameObject component, int pointer, string count)
    {
        Text text = component.transform.GetChild(pointer).GetChild(1).transform.GetComponent<Text>();
        text.text = count;
    }

    public void ToggleColor(GameObject component, int pointer, string color)
    {
        Text text1 = component.transform.GetChild(pointer).GetChild(0).transform.GetComponent<Text>();
        Text text2 = component.transform.GetChild(pointer).GetChild(1).transform.GetComponent<Text>();
        if (color == "black")
        {
            text1.color = Color.black;
            text2.color = Color.black;
        }
        else if (color == "blue")
        {
            text1.color = Color.blue;
            text2.color = Color.blue;
        }
        else
        {
            text1.color = Color.red;
            text2.color = Color.red;
        }
    }

    public GameObject Match(int type)
    {
        GameObject component;
        if (type == 2)
        {  
            component=_menu;
        }
        else if (type == 3)
        {
            component=_inventorylist;
        }
        else if (type == 4)
        {
            component=_order;
        }
        else
        {
            component=_computer;
        }
        return component;
    }

    public GameObject MatchBackground(int type)
    {
        GameObject component;
        if (type == 2)
        {  
            component=_menuUI;
        }
        else if (type == 3)
        {
            component=_inventoryUI;
        }
        else if (type == 4)
        {
            component=_orderUI;
        }
        else
        {
            component=_computerUI;
        }
        return component;
    }

    public void ShowMenu(int type, Action onNpcDialogFinish = null)
    {
        _state = type;
        _onCurrentDialogFinish = onNpcDialogFinish;
        _currentDialogLine = 0;
        
        index = 0;
        pointer = 0;

        GameObject component = Match(type);
        childCount=component.transform.childCount;
        ToggleColor(component, pointer, "blue");
        ToggleMenu(true, type);
        OnDialogStart?.Invoke();
    }

    public void EndMenu(int type)
    {
        GameObject component = Match(type);
        Vector3 coordinate = component.transform.position;
        coordinate.y=coordinate.y-scroll*pointer;
        component.transform.position=coordinate;
        ToggleColor(component, pointer, "black");
        ToggleMenu(false, type);
        OnDialogFinish?.Invoke();
        _onCurrentDialogFinish?.Invoke();
        _state = 0;
    }
    
    public void StartDialog(Action onNpcDialogFinish = null)
    {
        _state = 1;
        _currentDialog = _dialog;
        _onCurrentDialogFinish = onNpcDialogFinish;
        _currentDialogLine = 0;
        ShowCurrentDialogLine();
        OnDialogStart?.Invoke();
    }

    public void EndDialog()
    {
        ToggleDialogBox(false);
        OnDialogFinish?.Invoke();
        _onCurrentDialogFinish?.Invoke();
        _state = 0;
    }

    private void ToggleMenu(bool active, int type)
    {
        _board.SetActive(active);
        GameObject component = MatchBackground(type);
        component.SetActive(active);
    }
    
    private void ToggleDialogBox(bool active)
    {
        _dialogBox.SetActive(active);
    }

    private void ShowCurrentDialogLine()
    {
        StartCoroutine(AnimateDialogLine(_currentDialog.Lines[_currentDialogLine]));
    }

    private IEnumerator AnimateDialogLine(string line)
    {
        var lastSoundTime = Time.time;

        ToggleDialogBox(true);

        _dialogText.text = "";
        _dialogTextTween = DOTween.To(() => _dialogText.text, x => _dialogText.text = x, line, line.Length / _dialogSpeed)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                if (Time.time > lastSoundTime + _dialogSFX.length && _dialogText.text[Math.Max(_dialogText.text.Length - 1, 0)] != ' ')
                {
                    AudioManager.SharedInstance.PlaySFX(_dialogSFX);
                    lastSoundTime = Time.time;
                }
            })
            .OnComplete(() =>
            {
                _dialogTextTween = null;
                _dialogText.text = $"{_dialogText.text} ⇩";
            });

        yield return _dialogTextTween.WaitForCompletion();
    }
}
