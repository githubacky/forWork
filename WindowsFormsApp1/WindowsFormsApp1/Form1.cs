using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DataTable tdTable = new DataTable("Table"); // バッファとしてDataTableを設置
        private readonly string csvName = "TodoList.csv";
        private readonly string dateFormat = "yyyy/MM/dd";
        private readonly string textEncoding = "UTF-8";
        private int statusEnable = 1;   // イベントによるステータスバー更新の有効フラグ

        private const int IDX_FIN = 0; //「完了」列のインデックス
        private const int IDX_DISP = 3; //「表示」列のインデックス

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// フォーム起動時処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // ☆☆☆☆ --------------------------------------------------
            //dataGridView1.RowHeadersVisible = false;
            // 起動時はイベントによるステータスバー更新を無効化
            statusEnable = 0;

            // 保存済のcsvデータを読み込む(メソッド化)
            tdTable = WindowsFormsAppParts.CsvFileManager.CsvFileInitialRead(csvName);

            // DataTableとDataGridViewをバインド
            dataGridView1.DataSource = tdTable;

            // 変更を確定
            tdTable.AcceptChanges();

            // 初期表示設定
            InitView();

            // イベントによるステータスバー更新を有効に戻す
            statusEnable = 1;

            // イベント登録
            dataGridView1.CellContentClick +=
                new System.Windows.Forms.DataGridViewCellEventHandler(DataGridView1_CellContentClick);    // 完了タスクの非表示用
            dataGridView1.CellValidating +=
                new System.Windows.Forms.DataGridViewCellValidatingEventHandler(DataGridView1_CellValidating); // 入力済タスクの修正用
            dataGridView1.CellValidated +=
                new System.Windows.Forms.DataGridViewCellEventHandler(DataGridView1_CellValidated);    // 入力済タスクの修正用

        }

        /// <summary>
        /// DataGridViewの初期表示に関する設定をする。
        /// </summary>
        private void InitView()
        {
            // DataGridViewの書式設定
            dataGridView1.Columns[IDX_DISP].Visible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;   // 列幅の自動調整
            dataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;  // タイトルを中央揃え
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;  // セル選択時、行全体を選択
            dataGridView1.AllowUserToAddRows = false;   // 手動での行追加を禁止

            // 表示非表示の設定
            // 初期表示では完了タスクを非表示にする
            foreach (DataRow dr in tdTable.Rows)
            {
                if ((bool)dr[IDX_FIN] == true)
                {
                    // 完了タスクがtrueだったら
                    // 表示はfalse(=非表示)にする
                    dr[IDX_DISP] = false;

                }
                else
                {
                    // 完了タスク上記以外
                    // 表示はtrue(=表示)にする
                    dr[IDX_DISP] = true;
                }
            }
            // RowFillerの設定(表示列がtrueの行のみ表示する)
            tdTable.DefaultView.RowFilter = "表示 = true";
        }


        // ☆☆ ----------------------------------------------------------
        /// <summary>
        /// 入力したタスクをDataGridViewに登録する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            // フォームデータ(期限、やること)を読み込む
            DateTime dateTime = dateTimePicker1.Value;
            String lim = dateTime.Date.ToString(dateFormat);
            String toDo = textBox1.Text;

            // やること欄が空欄でないかをチェック
            if (string.IsNullOrEmpty(toDo))
            {
                MessageBox.Show("やること欄が空欄です。", "確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
            else
            {

                // Rows.Addメソッドを使ってデータを追加
                tdTable.Rows.Add(false, lim, toDo, true);
                
            }

        }

        /// <summary>
        /// 選択したタスクを削除する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count <= 0)
            {
                // 削除対象タスクが選択されているかをチェック
                MessageBox.Show("タスクが選択されていません。", "確認",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
            else
            {
                DialogResult result = MessageBox.Show("削除してよろしいですか？", "確認",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                // OKボタンを押したら削除実行
                if (result == DialogResult.OK)
                {
                    foreach (DataGridViewRow dgr in dataGridView1.SelectedRows)
                    {
                        DataRowView drv = (DataRowView)dgr.DataBoundItem;
                        DataRow dr = (DataRow)drv.Row;
                        if (dr.RowState == DataRowState.Added)
                        {
                            // 追加された行に対しては変更をキャンセル
                            dr.RejectChanges();
                        }
                        else
                        {
                            dr.Delete();
                            dr.AcceptChanges();
                        }
                    }
                    DataGridView1_Statusbar_Update();
                }
            }
            
        }

        /// <summary>
        /// 入力済のタスクを修正する際の動作を規定する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            // 新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }

            // 日付部分の修正対応
            if (dgv.Columns[e.ColumnIndex].Index == 1)
            {
                string target = e.FormattedValue.ToString();
                string afterData; // = "";
                //DateTime? dbg = null;

                try
                {
                    // 日付修正後の月日表示を2桁表示にするための処理
                    afterData = DateTime.Parse(target).ToString(dateFormat);
                    tdTable.Rows[e.RowIndex][e.ColumnIndex] = afterData;
                    // -> 月日を1桁で修正しても2桁に戻す
                    // ex.)2020/4/28 -> 2020/04/28に修正
                }
                catch (Exception)
                {
                    // 行にエラーテキストを設定＆ダイアログ表示
                    string errMsg = "不正な入力値です";
                    dgv.Rows[e.RowIndex].ErrorText = errMsg;
                    MessageBox.Show(errMsg, "警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    //入力した値をキャンセルして元に戻す
                    dgv.CancelEdit();
                    //キャンセルする
                    e.Cancel = true;
                }

            }

            // タスク部分の修正対応
            if (dgv.Columns[e.ColumnIndex].Index == 2 && e.FormattedValue.ToString() == "")
            {
                // 行にエラーテキストを設定＆ダイアログ表示
                string errMsg = "値が入力されていません。";
                dgv.Rows[e.RowIndex].ErrorText = errMsg;
                MessageBox.Show(errMsg, "警告",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                // 入力した値をキャンセルして元に戻す
                dgv.CancelEdit();
                // キャンセルする
                e.Cancel = true;
            }

        }

        /// <summary>
        /// 行選択解除したらエラーテキストを消す
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;
            //エラーテキストを消す
            dgv.Rows[e.RowIndex].ErrorText = null;
        }


        // ☆☆☆ --------------------------------------------------------
        /// <summary>
        /// 登録タスクをcsvファイル形式で保存する。
        /// 保存先：exe実行フォルダ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("保存してよろしいですか？", "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
            // OKボタンを押したら削除実行
            if (result == DialogResult.OK)
            {
                try
                {
                    // csvファイルパス指定
                    // -- 当初は保存先をダイアログで選択するようにしていたが、
                    // -- ユーザビリティの観点から保存プロセスをユーザに見せないようにした。
                    string csvPath = Directory.GetCurrentDirectory() + "\\" + csvName;

                    // エンコード形式設定
                    System.Text.Encoding enc =
                        System.Text.Encoding.GetEncoding(textEncoding);
                    // 書き込むファイルを開く
                    System.IO.StreamWriter sw =
                        new System.IO.StreamWriter(csvPath, false, enc);
                    // 列情報取得
                    int colCount = tdTable.Columns.Count;
                    // データ書き込み(メソッド化)
                    WindowsFormsAppParts.CsvFileManager.CsvFileWrite(tdTable.Rows, colCount, sw);
                    // 終了のお知らせ
                    MessageBox.Show("保存しました", "情報",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("タスク保存ファイルが既に開いています。\r\n" +
                        "ファイルを閉じてください。", "警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// 非表示にしたリストを再表示する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRedisplay_Click(object sender, EventArgs e)
        {
            // ステータスバーの自動更新を一時的にOFFする
            statusEnable = 0;

            // 表示設定
            foreach (DataRow dr in tdTable.Rows)
            {
                // すべてtrue(=表示)にする
                dr[IDX_DISP] = true;
            }

            // フィルタ解除
            tdTable.DefaultView.RowFilter = "";

            // ステータスバー更新
            DataGridView1_Statusbar_Update();

            // ステータスバーの自動更新を元に戻す
            statusEnable = 1;
        }

        /// <summary>
        /// 完了タスクを非表示にする。
        /// 完了ボタンチェックを感知して実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // ステータスバーの自動更新を一時的にOFFする
            statusEnable = 0;

            // DataViewのフィルタ条件を設定（削除行は表示しない）
            tdTable.DefaultView.RowStateFilter = DataViewRowState.Added
                     | DataViewRowState.ModifiedCurrent | DataViewRowState.Unchanged;

            if (e.ColumnIndex == 0 && e.RowIndex != -1)
            {
                try
                {
                    bool beforeState = (bool)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    if (beforeState == false)
                    {
                        // バインドしているデータを取得
                        DataGridViewRow dgr = this.dataGridView1.CurrentRow;
                        DataRowView drv = (DataRowView)dgr.DataBoundItem;
                        DataRow dr = (DataRow)drv.Row;

                        dr[IDX_FIN] = true;   // 完了済みにする
                        dr[IDX_DISP] = false; // 非表示に設定 
                    }
                    else
                    {
                        // "完了"チェックを外した時、行選択されたままだと
                        // 再度チェックした時に非表示にならないため、行選択を解除する
                        dataGridView1.CurrentCell = null;
                    }
                    tdTable.DefaultView.RowFilter = "表示 = true";
                    // ステータスバー更新
                    DataGridView1_Statusbar_Update();
                }
                catch (System.InvalidCastException)
                {
                    return;
                }
            }

            // ステータスバーの自動更新を元に戻す
            statusEnable = 1;
        }


        // ☆☆☆☆ ------------------------------------------------------
        /// <summary>
        /// 保存したタスクデータを再読み込みする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnReload_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("表示中のタスクをリセットし、\r\n" +
                "最後に保存した状態のタスクを読み込みます。\r\nよろしいですか？", "確認",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Exclamation,
                MessageBoxDefaultButton.Button1);
            // OKボタンを押したら削除実行
            if (result == DialogResult.OK)
            {
                tdTable = WindowsFormsAppParts.CsvFileManager.CsvFileInitialRead(csvName);
                dataGridView1.DataSource = tdTable;
                dataGridView1.CurrentCell = null;
            }
        }


        // ----------------------------------------------------------------------
        // ステータスバー操作
        // ----------------------------------------------------------------------
        /// <summary>
        /// dataGridViewの情報をステータスバーに反映する。
        /// </summary>
        private void DataGridView1_Statusbar_Update()
        {
            int taskCount = tdTable.Rows.Count;   // 登録タスク数
            // 完了タスク数
            int finCount = tdTable.Select("完了=true").Length;
            // 非表示タスク数
            int undispCount = tdTable.Select("表示=false").Length;

            // ステータスバーに表示
            toolStripStatusLabel1.Text =
                string.Format("タスク数：{0} / 完了タスク：{1}（うち非表示 {2}）",
                taskCount, finCount, undispCount);
        }

        /// <summary>
        /// dataGridView内の行が追加/削除された時にステータスバーを更新する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Statusbar_Update(object sender, EventArgs e)
        {
            if (statusEnable == 1)
            // フォームLoad時は実行しない(Form1_Load内で実行)
            {
                DataGridView1_Statusbar_Update();
            }
            
        }

    }
    
}


 


