using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CoinRunnerGameTests
{
    private PlayerMovement player;
    private Rigidbody2D body;
    private static readonly MethodInfo ReadInput = typeof(PlayerMovement).GetMethod("ReadInput", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo PhysicsStep = typeof(PlayerMovement).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

    [UnitySetUp]
    public IEnumerator LoadLevel()
    {
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync("SampleScene");
        yield return null;
        player = Object.FindFirstObjectByType<PlayerMovement>();
        Assert.IsNotNull(player);
        body = player.GetComponent<Rigidbody2D>();
        // Inject input at the controller boundary; physics and interactions still run normally.
        player.enabled = false;
    }

    private IEnumerator Steps(int count, float horizontal = 0, bool jump = false, bool release = false)
    {
        for (int i = 0; i < count; i++)
        {
            ReadInput.Invoke(player, new object[] { horizontal, jump && i == 0, release && i == 0 });
            PhysicsStep.Invoke(player, null);
            yield return new WaitForFixedUpdate();
        }
    }

    private void Place(Vector2 position)
    {
        body.position = position;
        player.ResetMotion();
        Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator LevelLayoutAndReferencesMatchLab()
    {
        var map = GameObject.Find("Ground").GetComponent<Tilemap>();
        int tiles = 0;
        foreach (var cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) tiles++;
        Assert.AreEqual(76, tiles);
        Assert.IsFalse(map.HasTile(new Vector3Int(7, -4, 0)));
        Assert.IsFalse(map.HasTile(new Vector3Int(8, -4, 0)));
        Assert.AreEqual(LayerMask.NameToLayer("Ground"), map.gameObject.layer);
        Assert.AreEqual(6, GameObject.FindGameObjectsWithTag("Coin").Length);
        Assert.AreEqual(3, GameObject.FindGameObjectsWithTag("Hazard").Length);
        Assert.AreEqual(0, player.GetComponent<CapsuleCollider2D>().sharedMaterial.friction);
        Assert.IsNotNull(GameManager.instance.coinText.font);
        Assert.AreEqual("Coins: 0 / 6", GameManager.instance.coinText.text);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerLandsRunsStopsAndFacesBothDirections()
    {
        yield return Steps(35);
        Assert.IsTrue(player.IsGrounded);
        Assert.That(body.position.y, Is.EqualTo(-2).Within(.08f));
        yield return Steps(10, 1);
        Assert.Greater(body.position.x, -5);
        Assert.IsFalse(player.GetComponent<SpriteRenderer>().flipX);
        Assert.Greater(player.facingMarker.localPosition.x, 0);
        yield return Steps(10, -1);
        Assert.IsTrue(player.GetComponent<SpriteRenderer>().flipX);
        Assert.Less(player.facingMarker.localPosition.x, 0);
        yield return Steps(2);
        Assert.That(body.linearVelocity.x, Is.EqualTo(0).Within(.001f));
    }

    [UnityTest]
    public IEnumerator JumpCannotRepeatInAirAndEarlyReleaseCutsHeight()
    {
        yield return Steps(35);
        yield return Steps(2, jump: true);
        Assert.Greater(body.linearVelocity.y, 10);
        float before = body.linearVelocity.y;
        yield return Steps(1, jump: true);
        Assert.Less(body.linearVelocity.y, before);
        before = body.linearVelocity.y;
        yield return Steps(1, release: true);
        Assert.Less(body.linearVelocity.y, before * .6f);
        Assert.Greater(body.linearVelocity.y, 0);
    }

    [UnityTest]
    public IEnumerator LowAndHighPlatformsAreReachable()
    {
        Place(new Vector2(1, -2));
        yield return Steps(3);
        yield return Steps(17, 1, jump: true);
        yield return Steps(120);
        Assert.IsTrue(player.IsGrounded);
        Assert.That(body.position.y, Is.EqualTo(1).Within(.12f));
        Place(new Vector2(4, 1));
        yield return Steps(3);
        yield return Steps(77, 1, jump: true);
        yield return Steps(120);
        Assert.IsTrue(player.IsGrounded);
        Assert.That(body.position.y, Is.EqualTo(3).Within(.12f));
    }

    [UnityTest]
    public IEnumerator GapCanBeJumped()
    {
        Place(new Vector2(6.6f, -2));
        yield return Steps(3);
        yield return Steps(25, 1, jump: true);
        yield return Steps(120);
        Assert.Greater(body.position.x, 9);
        Assert.IsTrue(player.IsGrounded);
    }

    [UnityTest]
    public IEnumerator CoinTriggersCountOnceAndSurviveRespawn()
    {
        foreach (var coin in GameObject.FindGameObjectsWithTag("Coin"))
        {
            Place(coin.transform.position);
            yield return Steps(2);
            yield return null;
        }
        Assert.AreEqual("Coins: 6 / 6", GameManager.instance.coinText.text);
        Assert.AreEqual(0, GameObject.FindGameObjectsWithTag("Coin").Length);
        Place(new Vector2(8, -11));
        yield return Steps(3);
        yield return null;
        yield return null;
        Assert.That(Vector2.Distance(body.position, new Vector2(-6, -2)), Is.LessThan(.1f));
        Assert.AreEqual("Coins: 6 / 6", GameManager.instance.coinText.text);
    }

    [UnityTest]
    public IEnumerator SolidSpikeRespawnsAndClearsVelocity()
    {
        Place(new Vector2(10, -2));
        yield return Steps(18, 1);
        Assert.Less(body.position.x, -4);
        Assert.That(body.position.y, Is.EqualTo(-2).Within(.15f));
    }

    [UnityTest]
    public IEnumerator PlatformMovesReversesAndCarriesRunner()
    {
        var platform = Object.FindFirstObjectByType<MovingPlatform>();
        Place(platform.transform.position + new Vector3(0, 1.2f, 0));
        yield return Steps(10);
        Assert.IsTrue(player.IsGrounded);
        float offset = body.position.x - platform.transform.position.x;
        float start = platform.transform.position.x;
        yield return Steps(35);
        Assert.Greater(platform.transform.position.x, start + .5f);
        Assert.That(body.position.x - platform.transform.position.x, Is.EqualTo(offset).Within(.2f));
        yield return Steps(140);
        Assert.Less(platform.Velocity.x, 0);
        Assert.That(platform.transform.position.x, Is.InRange(8f, 11.01f));
    }

    [UnityTest]
    public IEnumerator CameraFollowsRunnerAtCorrectDepth()
    {
        Place(new Vector2(13, 5));
        yield return new WaitForSeconds(.8f);
        Assert.That(Camera.main.transform.position.x, Is.EqualTo(body.position.x).Within(1));
        Assert.That(Camera.main.transform.position.z, Is.EqualTo(-10).Within(.01f));
    }

    [UnityTest]
    public IEnumerator GoalShowsWinPanelAndButtonRestartsWithZeroCoins()
    {
        GameManager.instance.AddCoin();
        Place(GameObject.FindGameObjectWithTag("Goal").transform.position);
        yield return new WaitForFixedUpdate();
        yield return null;
        Assert.IsTrue(GameManager.instance.winPanel.activeSelf);
        Assert.AreEqual(0, Time.timeScale);
        var button = GameManager.instance.winPanel.GetComponentInChildren<Button>();
        Assert.AreEqual(1, button.onClick.GetPersistentEventCount());
        button.onClick.Invoke();
        yield return null;
        yield return null;
        Assert.AreEqual(1, Time.timeScale);
        Assert.IsFalse(GameManager.instance.winPanel.activeSelf);
        Assert.AreEqual("Coins: 0 / 6", GameManager.instance.coinText.text);
        Assert.AreEqual(6, GameObject.FindGameObjectsWithTag("Coin").Length);
    }
}
