using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTrackingTags : MonoBehaviour
{
    [SerializeField]
    private List<EyeTrackingTag> tagsList;

    public List<EyeTrackingTag> AllTags => tagsList;
    public List<string> AllTagsListStrings()
    {
        List<string> l = new List<string>();
        foreach (EyeTrackingTag t in tagsList) { l.Add(t.name);}
        return l;
    }

    public bool HasTag(EyeTrackingTag t) { return tagsList.Contains(t); }

    public bool HasTag(string tagName) { return tagsList.Exists(t => t.name == tagName); }
}
