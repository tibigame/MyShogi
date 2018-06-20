﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MyShogi.Model.Common.ObjectModel;

namespace MyShogi.View.Win2D
{
    public partial class KifuControl : UserControl
    {
        public KifuControl()
        {
            InitializeComponent();

            // 文字色を変えたいのでowner drawにする。
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;

#if false // うまくいかないのでfalse
            // 棋譜ウィンドウがちらつくの嫌なのでダブルバッファリングにする。
            this.SetStyle(ControlStyles.DoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
#endif
        }

        /// <summary>
        /// リストが変更されたときに呼び出されるハンドラ
        /// </summary>
            public void OnListChanged(PropertyChangedEventArgs args)
        {
            if (!IsHandleCreated)
                return;

            Invoke(new Action(() =>
            {
                // ここでListBoxをいじって、listBox1_SelectedIndexChanged()が呼び出されるのは嫌だから抑制する。

                listBox1.SelectedIndexChanged -= listBox1_SelectedIndexChanged;

                List<string> list = args.value as List<string>;

                int start;
                if (args.start == -1)
                    start = 0; // 丸ごと更新された
                else
                    start = args.start; // 部分更新された

                // endの指定は無視される。

                var listbox = listBox1;
                listbox.BeginUpdate();

                int j = -1;

                // 値の違う場所のみ書き換える
                // 値の違うところを探す
                // start以降、endまでしか更新されていないことは保証されているものとする。

                // デバッグ用に、startまで要素が足りていなければとりあえず埋めておく。
                for (int i = listbox.Items.Count; i < start; ++i)
                    listbox.Items.Add(list[i]);

                for (int i = start; i < list.Count ; ++i)
                {
                    if (listbox.Items.Count <= i || list[i] != listbox.Items[i].ToString())
                    {
                        // ここ以降を書き換える。
                        while (listbox.Items.Count > i)
                            listbox.Items.RemoveAt(listbox.Items.Count -1); // RemoveLast

                        j = i; // あとでここにフォーカスを置く
                        for(; i < list.Count; ++i)
                            listbox.Items.Add(list[i]);

                        break;
                    }
                }

                // ここまで完全一致なので、末尾にフォーカスがあって良い。
                if (j == -1)
                    j = list.Count - 1;

                // そのあとの要素が多すぎるなら削除する。(ユーザーが待ったした時などにそうなる)
                while (listbox.Items.Count > list.Count)
                    listbox.Items.RemoveAt(listbox.Items.Count - 1); // RemoveLast


                // カーソルを異なる項目が最初に見つかったところに置いておく。
                listbox.SelectedIndex = j;

                listbox.EndUpdate();

                listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;

            }));
        }

        /// <summary>
        /// 棋譜の読み込み時など、LocalServer側の要請により、棋譜ウィンドウを指定行に
        /// フォーカスを当てるためのハンドラ
        /// </summary>
        public void SetKifuListIndex(int selectedIndex)
        {
            if (!IsHandleCreated)
                return;

            Invoke(new Action(() =>
            {
                if (listBox1.Items.Count <= selectedIndex)
                    selectedIndex = listBox1.Items.Count - 1;
                listBox1.SelectedIndex = selectedIndex;
            }));
        }

        // -- 以下、棋譜ウインドウに対するオペレーション

        /// <summary>
        /// 親ウインドウがリサイズされた時にそれに収まるようにこのコントロール内の文字の大きさを
        /// 調整する。
        /// </summary>
        /// <param name="scale"></param>
        public void OnResize(double scale)
        {
            // 最小化したのかな？
            if (this.Width == 0 || listBox1.ClientSize.Width == 0)
                return;

            // 画面を小さくしてもスクロールバーは小さくならないから計算通りのフォントサイズだとまずいのか…。
            var font_size = (float)(22 * scale);

            /*
                // ClientSizeはスクロールバーを除いた幅なので、controlのwidthとの比の分だけ
                // fontを小さく表示してやる。
                font_size *= (float)listBox1.ClientSize.Width / this.Width;

                Console.WriteLine(listBox1.ClientSize.Width + "/" + this.Width);
            */

            // スクロールバーが出たために文字の横幅が収まらない可能性を考慮してItems.Add()ごとに
            // OnResize()を都度呼び出してやりたい…が、スクロールバーが出た結果、文字を縮小して、
            // その結果、スクロールバーが消えるという現象が…。

            // →　結論的には、スクロールバーの有無によって文字フォントサイズを変更するのは
            // 筋が良くないということに。最初からスクロールバーの分だけ控えて描画すべき。

            // ところがスクロールバーの横幅不明。実測34だったが、環境によって異なる可能性が..
            font_size *= ((float)Width - 34 /* scroll bar */) / Width;

            // 前回のフォントサイズと異なるときだけ再設定する
            if (last_font_size == font_size)
                return;

            last_font_size = font_size;
            last_scale = scale;

            listBox1.Font = new Font("MS Gothic", font_size, FontStyle.Regular , GraphicsUnit.Pixel);

            // font変更の結果、選択しているところがlistboxの表示範囲外になってしまうことがある。
            // これ、あとで修正を考える。
        }

        private double last_scale = 0;
        private float last_font_size = 0;

        /// <summary>
        /// 選択行が変更されたので、ViewModelにコマンドを送信してみる。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedIndexChangedHandler != null)
                SelectedIndexChangedHandler(listBox1.SelectedIndex);
        }

        public delegate void SelectedIndexChangedEvent(int selectedIndex);

        // 棋譜ウィンドウの選択行が変更になった時に呼び出されるハンドラ
        public SelectedIndexChangedEvent SelectedIndexChangedHandler;

        // 棋譜ウィンドウの各行の色をカスタムに変更したいので、描画ハンドラを自前で書く。
        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index > -1)
            {
                Brush wBrush = null;
                try
                {
                    if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
                    {
                        /*                        
                                                //選択されていない行
                                                if (e.Index < 2)
                                                {
                                                    //指定された行より小さければ青
                                                    wBrush = new SolidBrush(Color.Blue);
                                                }
                                                else
                                                {
                                                    //指定された行より大きければ通常色
                                                    wBrush = new SolidBrush(e.ForeColor);
                                                }
                        */
                        // あとで考える。

                        wBrush = new SolidBrush(e.ForeColor);
                    }
                    else
                    {
                        //選択されている行なら通常色
                        wBrush = new SolidBrush(e.ForeColor);
                    }
                    //文字を設定
                    e.Graphics.DrawString(((ListBox)sender).Items[e.Index].ToString(), e.Font, wBrush, e.Bounds);
                }
                finally
                {
                    if (wBrush != null)
                    {
                        wBrush.Dispose();
                    }
                }
            }
            e.DrawFocusRectangle();
        }
    }
}
