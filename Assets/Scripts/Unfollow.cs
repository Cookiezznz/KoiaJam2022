using System;
using UnityEngine;

public class Unfollow : MonoBehaviour
{
    public static event Action CutNodes;
    
    public Vector2 cutStartPos;
    public Vector2 cutEndPos;
    public float minCutDistance; //TODO Move to settings
    public float maxCutDistance; //TODO Move to settings
    
    public float unfollowCooldown; //TODO Move to settings
    
    private float _unfollowCooldownEndTime = 0;
    
    public bool cutStarted;

    public LayerMask linkLayerMask;

    public UnfollowVFX unfollowVFX;

    public void StartCut(Vector2 newCutStartPos)
    {
        //Is it on cooldown?
        if (Time.time < _unfollowCooldownEndTime)
        {
            Debug.Log("Unfollow on cooldown: " + (GetCooldownRemaining()) + " remaining");
            return;
        }

        cutStarted = true;
        cutStartPos = newCutStartPos;

        //Start VFX
        unfollowVFX.ShowFX();
    }

    //Called when the mouse is released after a hold while Unfollow is selected
    public bool EndCut(Vector2 newCutEndPos)
    {
        if (!cutStarted) return false; //Cut hasn't been started, why is one being ended?
        cutEndPos = newCutEndPos;
        
        //If cursor position is closer than minCutDistance on cursor deselect, deselect the cut
        float distance = Vector2.Distance(cutStartPos, cutEndPos);
        if (distance < minCutDistance)
        {
            unfollowVFX.EndFX(false);
            return false;
        }

        //Valid line, the misinformation has been sealed forever... haha jk... unless..?
        Debug.DrawLine(cutStartPos, cutEndPos, Color.red, Mathf.Infinity);
        
        //Links are on Z:5
        Vector3 offset = new Vector3(0, 0, 5);
        //Check for link collisions
        RaycastHit2D[] hits = Physics2D.RaycastAll(cutStartPos, cutEndPos - cutStartPos, distance, linkLayerMask, 0, 6);

        
        //for each link hit
        int linksUnfollowed = 0;
        foreach (RaycastHit2D hit in hits)
        {
            Link link = hit.transform.GetComponent<Link>();

            if (link)
            {
                linksUnfollowed++;
                link.LinkUnfollowed();
            }
        }

        bool success = linksUnfollowed > 0;
        unfollowVFX.EndFX(success);
        //Start cooldown if at least one link is broken.
        if (success)
        {
            
            StartCooldown();
            CutNodes?.Invoke();
        }
        ClearCut();

        return true;
    }

    private void StartCooldown()
    {
        _unfollowCooldownEndTime = Time.time + unfollowCooldown;
        
        CooldownRadialManager.instance.unfollowCooldownRadial.CooldownStarted(unfollowCooldown);
    }

    private float GetCooldownRemaining()
    {
        return (_unfollowCooldownEndTime - Time.time);
    }

    public void ClearCut()
    {
        if (!cutStarted) return; //Stops double clears
        
        cutStarted = false;
        cutStartPos = Vector3.zero;
        cutEndPos = Vector3.zero;
    }


    public void CheckMaxDist(Vector2 mousePos)
    {
        if (!cutStarted) return;
        
        //Get distance between start and current position
        float dist = Vector2.Distance(cutStartPos, mousePos);

        //Hit max? End cut
        if (dist > maxCutDistance) EndCut(mousePos);
    }
    
    private void OnEnable()
    {
        GameController.StopGame += GameReset;
    }

    private void GameReset(string s)
    {
        _unfollowCooldownEndTime = Time.time;
    }

    private void OnDisable()
    {
        GameController.StopGame -= GameReset;
    }
}
