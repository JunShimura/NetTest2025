using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace JankenRoomEx01
{
    class S
    {
        public static void Main()
        {
            Console.WriteLine("JankenRoom");
            SocketServer();
            Console.ReadKey();
        }

        public static void SocketServer()
        {
            // IPアドレスやポートの設定
            byte[] bytes = new byte[1024];
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[1];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // ソケットの作成
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
            listener.Listen(10);

            Console.WriteLine("クライアントの接続を待っています...");

            // クライアント3つの接続を待つ
            Socket[] clients = new Socket[3];
            string[] playerNames = new string[3];
            for (int i = 0; i < 3; i++)
            {
                clients[i] = listener.Accept();
                int bytesRec = clients[i].Receive(bytes);
                playerNames[i] = Encoding.UTF8.GetString(bytes, 0, bytesRec).Replace("<EOF>", "").Trim();
                Console.WriteLine($"クライアント{i + 1}({playerNames[i]})が接続しました。");
            }

            // クライアントにじゃんけんのメッセージを送信
            string sendData = "じゃんけんゲーム！\r\n0:ぐう　1:ちょき　2:ぱあ\r\n";
            byte[] msg = Encoding.UTF8.GetBytes(sendData);
            foreach (var client in clients)
            {
                client.Send(msg);
            }

            // 各クライアントの手を受信
            int[] hands = new int[3];
            string[] handStrs = new string[3];
            for (int i = 0; i < 3; i++)
            {
                int bytesRec = clients[i].Receive(bytes);
                handStrs[i] = Encoding.UTF8.GetString(bytes, 0, bytesRec).Replace("<EOF>", "").Trim();
                Console.WriteLine($"{playerNames[i]}の手: {handStrs[i]}");
                if (!int.TryParse(handStrs[i].Substring(0, 1), out hands[i]))
                {
                    hands[i] = -1; // 無効
                }
            }

            // 判定ロジック
            string[] handNames = { "ぐう", "ちょき", "ぱあ" };
            string[] results = new string[3];
            List<int> winners = new List<int>();

            bool allSame = (hands[0] == hands[1]) && (hands[1] == hands[2]);
            bool allDifferent = (hands[0] != hands[1]) && (hands[1] != hands[2]) && (hands[0] != hands[2]);
            bool valid = hands[0] >= 0 && hands[1] >= 0 && hands[2] >= 0;

            if (!valid)
            {
                for (int i = 0; i < 3; i++)
                {
                    results[i] = "無効な手が入力されました。";
                }
            }
            else if (allSame || allDifferent)
            {
                for (int i = 0; i < 3; i++)
                {
                    results[i] = "あいこ（全員同じ手、または全員違う手）";
                }
            }
            else
            {
                // 2人が同じ手、1人が違う手 → 1人勝ち or 2人勝ち
                // ぐう(0) > ちょき(1), ちょき(1) > ぱあ(2), ぱあ(2) > ぐう(0)
                // どの手が2人、どの手が1人かを判定
                int[] counts = new int[3];
                for (int i = 0; i < 3; i++) counts[hands[i]]++;

                int hand2 = Array.IndexOf(counts, 2);
                int hand1 = Array.IndexOf(counts, 1);

                if (hand2 >= 0 && hand1 >= 0)
                {
                    // hand2: 2人の手, hand1: 1人の手
                    // hand1がhand2に勝つ場合 hand1が勝者
                    if ((hand1 + 1) % 3 == hand2)
                    {
                        // hand1が勝ち
                        for (int i = 0; i < 3; i++)
                        {
                            if (hands[i] == hand1)
                            {
                                results[i] = $"あなた({playerNames[i]})の勝ち！ 対戦相手: {playerNames[(i + 1) % 3]}, {playerNames[(i + 2) % 3]}";
                                winners.Add(i);
                            }
                            else
                            {
                                results[i] = $"あなた({playerNames[i]})の負け。 対戦相手: {playerNames[(i + 1) % 3]}, {playerNames[(i + 2) % 3]}";
                            }
                        }
                    }
                    else
                    {
                        // hand2が勝ち（2人勝ち）
                        for (int i = 0; i < 3; i++)
                        {
                            if (hands[i] == hand2)
                            {
                                results[i] = $"あなた({playerNames[i]})の勝ち！ 対戦相手: {playerNames[(i + 1) % 3]}, {playerNames[(i + 2) % 3]}";
                                winners.Add(i);
                            }
                            else
                            {
                                results[i] = $"あなた({playerNames[i]})の負け。 対戦相手: {playerNames[(i + 1) % 3]}, {playerNames[(i + 2) % 3]}";
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < 3; i++)
                    {
                        results[i] = "判定エラー";
                    }
                }
            }

            // 結果をクライアントに送信
            for (int i = 0; i < 3; i++)
            {
                string handInfo = $"あなたの手: {handNames[hands[i]]}\r\n";
                string othersInfo = $"他プレイヤー: ";
                for (int j = 0; j < 3; j++)
                {
                    if (i != j)
                        othersInfo += $"{playerNames[j]}({handNames[hands[j]]}) ";
                }
                string resultMsg = handInfo + othersInfo + "\r\n" + results[i] + "\r\n";
                clients[i].Send(Encoding.UTF8.GetBytes(resultMsg));
            }

            // ソケットの終了
            for (int i = 0; i < 3; i++)
            {
                clients[i].Shutdown(SocketShutdown.Both);
                clients[i].Close();
            }
            listener.Close();
        }
    }
}