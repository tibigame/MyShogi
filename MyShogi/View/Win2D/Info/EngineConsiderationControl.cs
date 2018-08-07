﻿using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using MyShogi.Model.Shogi.Converter;
using MyShogi.Model.Shogi.Core;
using MyShogi.Model.Shogi.Data;
using MyShogi.Model.Shogi.Usi;
using MyShogi.Model.Common.Utility;
using MyShogi.Model.Common.ObjectModel;
using MyShogi.App;

namespace MyShogi.View.Win2D
{
    /// <summary>
    /// エンジンの思考内容。
    /// 片側のエンジン分
    /// </summary>
    public partial class EngineConsiderationControl : UserControl
    {
        public EngineConsiderationControl()
        {
            InitializeComponent();
            
            if (!TheApp.app.DesignMode)
            {
                InitListView();
                InitKifuFormatter();
                InitNotifyObject();
            }
        }

        // -- properties

        /// <summary>
        /// 通知の発生するproperties
        /// </summary>
        public class EngineConsiderationNotifyObject : NotifyObject
        {
            /// <summary>
            /// [UI Thread] : UIのコンボボックスで選択されている候補手の数を返す。
            /// 検討モードではこれに基づいてMultiPVで思考する。
            /// </summary>
            public int MultiPV
            {
                get { return GetValue<int>("MultiPV"); }
                set { SetValue<int>("MultiPV", value); }
            }

            /// <summary>
            /// [UI Thread] : UI上で候補手のコンボボックスを表示するのか。
            /// </summary>
            public bool EnableMultiPVComboBox
            {
                get { return GetValue<bool>("EnableMultiPVComboBox"); }
                set { SetValue<bool>("EnableMultiPVComboBox", value); }
            }

            /// <summary>
            /// [UI Thread] マウスで読み筋がクリックされた時にrootSfenと読み筋がセットされる。
            /// </summary>
            public MiniShogiBoardData PvClicked
            {
                get { return GetValue<MiniShogiBoardData>("PvClicked"); }
                set { SetValue<MiniShogiBoardData>("PvClicked", value); }
            }

#if false
            // Evalの元の値を残していない即時反映無理..GlobalConfigを見に行く実装にしてある。いずれ修正するかも。

            /// <summary>
            /// [UI Thread] 検討ウィンドウで思考エンジンが後手番のときに評価値を反転させるか(自分から見た評価値にするか)のフラグ
            /// </summary>
            public bool NegateEvalWhenWhite
            {
                get { return GetValue<bool>("NegateEvalWhenWhite"); }
                set { SetValue<bool>("NegateEvalWhenWhite", value); }
            }
#endif
        }

        /// <summary>
        /// このControlから発生するpropertyの変更イベント。
        /// </summary>
        public EngineConsiderationNotifyObject Notify = new EngineConsiderationNotifyObject();

        /// <summary>
        /// 生成する棋譜文字列のフォーマット
        /// </summary>
        public IKifFormatterOptions kifFormatter
        {
            get; set;
        }

        /// <summary>
        /// [UI Thread] : 開始局面のsfen。
        /// これをセットしてからでないとAddInfo()してはならない。
        /// このsetterでは、PVのクリアも行う。
        /// </summary>
        /// <param name=""></param>
        public string RootSfen
        {
            get
            {
                return root_sfen;
            }
            set
            {
                root_sfen = value;
                if (root_sfen != null)
                    position.SetSfen(value);
                ClearItems();
            }
        }

        /// <summary>
        /// [UI Thread] : エンジン名を設定/取得する。
        /// 
        /// このコントロールの左上のテキストボックスに反映される。
        /// setterでは、ヘッダー情報、PVのクリアも行う。
        /// </summary>
        public string EngineName
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; ClearHeader(); ClearItems(); }
        }

        /// <summary>
        /// Rankingで並び替えるかどうかのフラグ
        /// RankingとはUSIの"info multipv X pv ..."のXのところの値。何番目の候補手であるか。
        /// 
        /// 検討モードの時はtrue。「着順」/「R順」ボタンを押すとtrue/false切り替わる。
        /// 
        /// [UI Thread] : setter
        /// </summary>
        public bool SortRanking {
            get { return sortRanking; }
            set {
                if (sortRanking != value)
                    UpdateSortRanking(value);
                sortRanking = value;
            }
        }
        private bool sortRanking;


        /// <summary>
        /// [UI Thread] : PVのクリア
        /// </summary>
        public void ClearItems()
        {
            listView1.Items.Clear();
            list_item_moves.Clear();
        }

        /// <summary>
        /// [UI Thread] : 読み筋を1行追加する。
        /// </summary>
        /// <param name="info"></param>
        public void AddThinkReport(UsiThinkReport info)
        {
            if (info.Moves != null || info.InfoString != null)
            {

                // -- 指し手文字列の構築

                // Positionクラスを用いて指し手文字列を構築しないといけない。
                // UI Threadからしかこのメソッドを呼び出さないことは保証されているので、
                // positionのimmutable性は保つ必要はなく、Position.DoMove()～UndoMove()して良いが、素直にCloneしたほうが速いと思われ..

                var pos = position.Clone();

                var kifuString = new StringBuilder();

                // kifuStringに文字列を追加するlocal method。
                // 文字列を追加するときに句切りのスペースを自動挿入する。
                void append(string s)
                {
                    if (kifuString.Length != 0)
                        kifuString.Append(' ');
                    kifuString.Append(s);
                }

                if (info.Moves != null)
                {
                    var moves = new List<Move>();
                    foreach (var move in info.Moves)
                    {
                        if (!pos.IsLegal(move))
                        {
                            if (move.IsSpecial())
                                append(move_to_kif_string(pos, move));
                            else if (move.To() != Square.NB)
                                // 非合法手に対してKIF2の文字列を生成しないようにする。(それが表示できるとは限らないので..)
                                // また、Square.NBはparseに失敗した文字列であるから、これは出力する意味がない。(あえて出力するなら元の文字列を出力してやるべき)
                                append($"非合法手({ move.Pretty()})");

                            break;
                        }
                        append(move_to_kif_string(pos, move));
                        moves.Add(move);
                        // このあと盤面表示用にmovesを保存するが、
                        // 非合法局面の指し手を渡すことは出来ないので、合法だとわかっている指し手のみを保存しておく。

                        pos.DoMove(move);
                    }
                }
                else
                {
                    kifuString.Append(info.InfoString); // この文字列を読み筋として突っ込む。
                }

                // -- listView.Itemsに追加

                // それぞれの項目、nullである可能性を考慮しながら表示すべし。
                // 経過時間、1/10秒まで表示する。
                // "info string"の文字列を表示する時は、info.Eval == nullでkifuStringにその表示すべき文字列が渡されてここに来るので注意。

                var elpasedTimeString = info.ElapsedTime == null ? null : info.ElapsedTime.ToString("hh':'mm':'ss'.'f");
                var ranking = info.MultiPvString == null ? "1" : info.MultiPvString;

                var depthString = info.Eval == null ? null
                    : (info.Depth != null && info.SelDepth != null) ? $"{info.Depth}/{info.SelDepth}"
                    : (info.Depth == null ? null : info.Depth.ToString());

                // 後手番の時に自分から見た評価値を表示する設定であるなら、評価値の表示を反転させる。
                // ここで表示している値、保存していないので即時反映は無理だわ…。まあ、これは仕様ということで…。
                var isWhite = position.sideToMove == Model.Shogi.Core.Color.WHITE;
                if (isWhite && TheApp.app.config.NegateEvalWhenWhite)
                {
                    if (info.Eval != null)
                        info.Eval = info.Eval.negate();
                }

                var evalString = info.Eval == null ? null : info.Eval.Eval.Pretty();
                var evalBound = info.Eval == null ? null : info.Eval.Bound.Pretty();
                kifuString.Append(info.MovesSuffix);
                var pvString = kifuString.ToString();

                var list = new[]{
                    ranking,                          // MultiPVの順
                    elpasedTimeString,                // 思考時間
                    depthString,                      // 探索深さ
                    info.NodesString ,                // ノード数
                    evalString,                       // 評価値
                    evalBound,                        // "+-"
                    pvString,                         // 読み筋
                };

                var item = new ListViewItem(list);

                if (sortRanking)
                {
                    int r;
                    if (!int.TryParse(ranking, out r) || r < 1)
                        r = 1;

                    while (listView1.Items.Count < r)
                    {
                        listView1.Items.Add(string.Empty);
                        list_item_moves.Add(null);
                    }

                    // r行目のところに代入
                    list_item_moves[r-1] = info.Moves;

                    // listView1.Items[r - 1] = item;

                    // r行目しか代入していないのに再描画でちらつく。
                    // ListView、ダブルバッファにしているにも関わらず。
                    // .NET FrameworkのListView、出来悪すぎない？

                    var old = listView1.Items[r - 1];
                    if (old.SubItems.Count == list.Length)
                    {
                        // 要素一つひとつ入替えてやる。
                        // これならちらつかない。なんなんだ、このバッドノウハウ…。

                        for (int i = 0; i < list.Length; ++i)
                            old.SubItems[i].Text = list[i];

                    } else
                    {
                        // 要素数が異なるので丸ごと入れ替える。
                        listView1.Items[r - 1] = item;
                    }

                }
                else
                {
                    listView1.Items.Add(item);
                    // 読み筋をここに保存しておく。(ミニ盤面で開く用) なければnullもありうる。
                    list_item_moves.Add(info.Moves);

                    try
                    {
                        // 検討ウィンドウの縦幅を縮めているとTopItemへの代入がぬるぽになる。
                        listView1.TopItem = item; // 自動スクロール
                    } catch { }
                }
            }

            // -- その他、nullでない項目に関して、ヘッダー情報のところに反映させておく。

            UpdateHeader(info);
        }

        /// <summary>
        /// [UI Thread] : ヘッダー情報をクリアする。
        /// </summary>
        public void ClearHeader()
        {
            UpdateHeader(new UsiThinkReport()
            {
                PonderMove = "",
                NodesString = "",
                NpsString = "",
                HashPercentageString = "",
            });
        }

        // -- handlers

        private void listView1_Resize(object sender, System.EventArgs e)
        {
            UpdatePvWidth();
        }

        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            // この現在選択されているところにある読み筋の指し手を復元して、イベントハンドラに移譲する。
            var selected = listView1.SelectedIndices;
            if (selected.Count == 0)
                return;// 選択されていない…

            // multi selectではないので1つしか選択されていないはず…。
            int index = selected[0]; // first
            if (index < list_item_moves.Count && list_item_moves[index]!=null /* info stringなどだとnullがありうる。*/)
                Notify.RaisePropertyChanged("PvClicked", new MiniShogiBoardData()
                {
                    rootSfen = root_sfen,
                    moves = list_item_moves[index]
                });
        }

        private void EngineConsiderationControl_Resize(object sender, System.EventArgs e)
        {
            int h = textBox1.Height + 3;
            listView1.Location = new Point(0, h);
            listView1.Size = new Size(ClientSize.Width, ClientSize.Height - h);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 選択項目がないと-1になるので、その時にMultiPV == 1になることを保証する。
            Notify.MultiPV = Math.Max(comboBox1.SelectedIndex + 1 , 1);
        }

        /// <summary>
        /// 「R順」ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            SortRanking ^= true;
        }

        // -- privates

        private void InitListView()
        {
            listView1.FullRowSelect = true;
            //listView1.GridLines = true;
            listView1.Sorting = SortOrder.None;
            listView1.View = System.Windows.Forms.View.Details;

            // ヘッダーのテキストだけセンタリング、実項目は右寄せしたいのだが、これをするには
            // オーナードローにする必要がある。面倒くさいので、ヘッダーのテキストにpaddingしておく。
            // またヘッダーの1列目のTextAlignは無視される。これは.NET FrameworkのListViewの昔からあるバグ。(仕様？)

            // MultiPVの値(1,…)
            var ranking = new ColumnHeader();
            ranking.Text = "R";
            ranking.Width = 40;
            ranking.TextAlign = HorizontalAlignment.Center;

            var thinking_time = new ColumnHeader();
            thinking_time.Text = "経過時間";
            thinking_time.Width = 140;
            thinking_time.TextAlign = HorizontalAlignment.Center;

            var depth = new ColumnHeader();
            depth.Text = "深さ ";
            depth.Width = 100;
            depth.TextAlign = HorizontalAlignment.Right;

            var node = new ColumnHeader();
            node.Text = "探索局面数";
            node.Width = 180;
            node.TextAlign = HorizontalAlignment.Right;

            var eval = new ColumnHeader();
            eval.Text = "評価値  ";
            eval.Width = 150;
            eval.TextAlign = HorizontalAlignment.Right;

            // 評価値のScoreBound
            var score_bound = new ColumnHeader();
            score_bound.Text = "+-";
            score_bound.Width = 50;
            score_bound.TextAlign = HorizontalAlignment.Center;

            var pv = new ColumnHeader();
            pv.Text = "読み筋";
            pv.Width = 0;
            pv.TextAlign = HorizontalAlignment.Left;
            // 読み筋の幅は残り全部。UpdatePvWidth()で調整される。

            var header = new[] { ranking , thinking_time, depth, node, eval, score_bound, pv };

            listView1.Columns.AddRange(header);

            //listView1.AutoResizeColumns( ColumnHeaderAutoResizeStyle.ColumnContent);
            // headerとcontentの文字長さを考慮して、横幅が自動調整されて水平スクロールバーで移動してくれるといいのだが、うまくいかない。よくわからない。
        }

        /// <summary>
        /// 読み筋のところに表示する棋譜文字列の生成器の初期化
        /// </summary>
        private void InitKifuFormatter()
        {
            kifFormatter = new KifFormatterOptions
            {
                color = ColorFormat.Piece,
                square = SquareFormat.FullWidthMix,
                samepos = SamePosFormat.KI2sp,
                //fromsq = FromSqFormat.Verbose,
                fromsq = FromSqFormat.KI2, // 移動元を入れると棋譜ウィンドウには入り切らないので省略する。
            };
        }

        private void InitNotifyObject()
        {
            // MultiPVの初期値
            var multiPV = TheApp.app.config.ConsiderationMultiPV;
            multiPV = Math.Max(multiPV , 1); // 1以上を保証する
            multiPV = Math.Min(multiPV, comboBox1.Items.Count); // comboBox1の項目数と同じまで。
            Notify.MultiPV = multiPV;

            // 候補手のコンボボックス
            Notify.AddPropertyChangedHandler("EnableMultiPVComboBox", (e) =>
            { UpdateMultiPVComboBox(Notify.EnableMultiPVComboBox); });
            Notify.RaisePropertyChanged("EnableMultiPVComboBox", false);

            // 後手の時に評価値を反転させるかのフラグ
            // Evalの元の値を残していない即時反映無理..
            //Notify.AddPropertyChangedHandler("NegateEvalWhenWhite", (e) =>
            //{
            //});
        }

        /// <summary>
        /// 「読み筋」の列の幅を調整する。
        /// </summary>
        private void UpdatePvWidth()
        {
            // Column 生成前
            if (listView1.Columns.Count == 0)
                return;

            int sum_width = 0;
            int i = 0;
            for (; i < listView1.Columns.Count - 1; ++i)
                sum_width += listView1.Columns[i].Width;

            // Columnsの末尾が「読み筋」の表示であるから、この部分は、残りの幅全部にしてやる。
            listView1.Columns[i /* is the last index*/].Width = ClientSize.Width - sum_width;
        }

        /// <summary>
        /// 指し手を読み筋に表示する棋譜文字列に変換する。
        /// </summary>
        /// <param name="p"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        private string move_to_kif_string(Position p, Move m)
        {
            // 特殊な指し手は、KIF2フォーマットではきちんと変換できないので自前で変換する。
            // 例えば、連続王手の千日手による反則勝ちが単に「千日手」となってしまってはまずいので。
            // (『Kifu for Windoiws』ではそうなってしまう..)
            return m.IsOk() ? kifFormatter.format(p, m) :
                (p.sideToMove == Model.Shogi.Core.Color.BLACK ? "☗":"☖") + m.SpecialMoveToKif();
        }

        /// <summary>
        /// [UI Thread] : ヘッダー情報のところに反映させる。
        /// nullの項目は書き換えない。
        /// </summary>
        /// <param name="info"></param>
        private void UpdateHeader(UsiThinkReport info)
        {
            // .NET FrameworkのTextBox、右端にスペースをpaddingしていて、TextAlignをcenterに設定してもそのスペースを
            // わざわざ除去してからセンタリングするので(余計なお世話)、TextAlignをLeftに設定して、自前でpaddingする。
            
            // MS UI Gothicは等幅ではないのでスペースでpaddingするとずれる。
            // TextBoxのフォントは、MS ゴシックに設定する。

            //textBox1.Text = info.PlayerName;

            if (info.PonderMove != null)
                textBox2.Text = $" 予想手 : { info.PonderMove.PadLeftUnicode(6)}";

            //textBox3.Text = $"探索手：{info.SearchingMove}";
            // 探索手、エンジン側、まともに出力してると出力だけで時間すごくロスするので
            // 出力してくるエンジン少なめだから、これ不要だと思う。

            //textBox4.Text = $"深さ：{info.Depth}/{info.SelDepth}";
            // 深さも各iterationごとにPVを出力しているわけで、こんなものは不要である。

            if (info.NodesString != null)
                textBox3.Text = $"探索局面数 : { info.NodesString.PadLeftUnicode(12) }";

            if (info.NpsString != null)
                textBox4.Text = $" NPS : { info.NpsString.PadLeftUnicode(12) }";

            if (info.HashPercentageString != null)
                textBox5.Text = $"HASH使用率 : { info.HashPercentageString.PadLeftUnicode(6) }";
        }

        /// <summary>
        /// [UI Thread] : 候補手のコンボボックスを表示するときにテキストボックスのレイアウトを変更する。
        /// EnableMultiPVComboBoxのsetterから呼び出される。
        /// </summary>
        /// <param name="enable"></param>
        private void UpdateMultiPVComboBox(bool enable)
        {
            // この順番で左上から右方向に並べる
            var list =
                enable ?
                 new Control[] { button1 , comboBox1, textBox1, textBox2, textBox3, textBox4, textBox5 } :
                 new Control[] { button1 , textBox1, textBox2, textBox3, textBox4, textBox5 };

            comboBox1.Visible = enable;

            // x座標は、一番左端にあるやつを基準とする。
            int x = int.MaxValue;
            foreach(var e in list)
                x = Math.Min(x, e.Location.X);

            // y座標は共通なのでtextBox1のあった位置で良い。
            int y = textBox1.Location.Y;

            foreach(var e in list)
            {
                e.Location = new Point(x, y);
                x += e.Size.Width + 4;
            }

            if (enable)
            {
                comboBox1.SelectedIndex = Notify.MultiPV -1;
            }
        }

        /// <summary>
        /// [UI Thread] : SortRankingの値が変わった時に呼び出される。
        /// </summary>
        /// <param name="sortRanking"></param>
        private void UpdateSortRanking(bool sortRanking)
        {
            button1.Text = sortRanking ? "R順" : "着順";
            ClearItems(); // 切り替えたので読み筋クリア
        }

        // -- private members

        /// <summary>
        /// 開始局面のsfen。
        /// この文字列とpositionの居面は合致している。
        /// RootSfenのsetterでセットされる。
        /// </summary>
        private string root_sfen;

        /// <summary>
        /// 内部に棋譜文字列の構築用に局面クラスを持っている。
        /// RootSfenのsetterでセットされる。
        /// </summary>
        private Position position = new Position();

        /// <summary>
        /// 表示している読み筋(ListView.Items)に対応する指し手
        /// </summary>
        private List<List<Move>> list_item_moves = new List<List<Move>>();

    }
}
