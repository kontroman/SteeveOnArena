using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMessageSender : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(2f);

        Devotion.Messages.Game.GameStarted.Publish("testString");
    }
}
