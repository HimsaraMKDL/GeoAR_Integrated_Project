using System;
using System.IO;
using UnityEngine;
using TMPro;

public class StudentProfileManager : MonoBehaviour
{
    // ??????? ???? ?? static property ??
    public static string CurrentStudentID { get; private set; }

    [Header("UI References")]
    [SerializeField]
    private TMP_InputField studentNameInput;

    [SerializeField]
    private GameObject studentEntryPanel;

    [SerializeField]
    private GeometryMenuManager geometryMenuManager;


    [Header("Settings")]
    [SerializeField]
    private int maxStudents = 50;


    private string dataFolder;


    private void Awake()
    {
        dataFolder =
            Path.Combine(
                Application.persistentDataPath,
                "ResearchData"
            );

        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
        }
    }


    public void StartLearning()
    {
        if (studentNameInput == null)
        {
            Debug.LogError(
                "Student Name Input is not assigned."
            );
            return;
        }


        string studentName =
            studentNameInput.text.Trim();


        if (string.IsNullOrEmpty(studentName))
        {
            Debug.LogWarning(
                "Please enter student name."
            );
            return;
        }


        if (GetStudentCount() >= maxStudents)
        {
            Debug.LogWarning(
                "Maximum student limit reached."
            );
            return;
        }


        string studentID =
            GenerateStudentID();


        SaveStudentProfile(
            studentID,
            studentName
        );

        // ??????? ???? ?? ????
        CurrentStudentID = studentID;

        Debug.Log(
            "Current Student ID: " +
            CurrentStudentID
        );

        Debug.Log(
            "Student Created: " +
            studentID
        );


        // Hide Entry Screen
        if (studentEntryPanel != null)
        {
            studentEntryPanel.SetActive(false);
        }


        // Open Main Menu
        if (geometryMenuManager != null)
        {
            geometryMenuManager.ShowMainMenu();
        }
    }



    private string GenerateStudentID()
    {
        int number =
            GetStudentCount() + 1;


        return
            "ST" +
            number.ToString("D3");
    }



    private void SaveStudentProfile(
        string id,
        string name)
    {
        StudentProfile profile =
            new StudentProfile();

        profile.studentID = id;
        profile.studentName = name;
        profile.createdTime =
            DateTime.Now.ToString();


        string json =
            JsonUtility.ToJson(
                profile,
                true
            );


        string filePath =
            Path.Combine(
                dataFolder,
                id + ".json"
            );


        File.WriteAllText(
            filePath,
            json
        );
    }



    private int GetStudentCount()
    {
        if (!Directory.Exists(dataFolder))
            return 0;


        string[] files =
            Directory.GetFiles(
                dataFolder,
                "ST*.json"
            );


        return files.Length;
    }
}



[Serializable]
public class StudentProfile
{
    public string studentID;

    public string studentName;

    public string createdTime;
}