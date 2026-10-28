using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    public Text TimerText;
    public Text ScoreText;
    public Text MessageText;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Object == null || !gm.Object.IsValid)
        {
            TimerText.text = "";
            ScoreText.text = "";
            MessageText.text = "";
            return;
        }

        var list = new List<Player>();
        foreach (var p in Player.All)
            if (p != null && p.Object != null && p.Object.IsValid) list.Add(p);
        list.Sort((a, b) => b.Score.CompareTo(a.Score));

        var sb = new StringBuilder("PLACAR\n");
        foreach (var p in list)
        {
            string hex = ColorUtility.ToHtmlStringRGB(p.BaseColor);
            sb.Append("<color=#").Append(hex).Append(">")
              .Append(p.Nick.ToString()).Append(": ").Append(p.Score).Append("</color>\n");
        }
        ScoreText.text = sb.ToString();

        switch (gm.State)
        {
            case GameState.Waiting:
                TimerText.text = "";
                MessageText.text = "Aguardando jogadores (minimo 2)... Conectados: " + list.Count;
                break;

            case GameState.Playing:
                TimerText.text = Mathf.CeilToInt(gm.TimeLeft).ToString();
                MessageText.text = "Colete moedas (+1) e acerte inimigos (+2)! Atordoados ficam parados.";
                break;

            case GameState.Finished:
                TimerText.text = "FIM";
                if (list.Count == 0) { MessageText.text = ""; break; }
                bool tie = list.Count > 1 && list[0].Score == list[1].Score;
                MessageText.text = tie
                    ? "EMPATE! Nova partida ja ja..."
                    : "VENCEDOR: " + list[0].Nick.ToString() + "! Nova partida ja ja...";
                break;
        }
    }
}
