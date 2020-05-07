using System;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Collections.Generic;

// csvファイル処理に関するメソッド
namespace WindowsFormsAppParts
{
    public partial class CsvFileManager
    {
        /// <summary>
        /// DataTableのデータをcsvファイルに書き込む。
        /// </summary>
        /// <param name="dtRows">テーブルの行データ</param>
        /// <param name="colCount">列数</param>
        /// <param name="sw">データ保存ファイル</param>
        public static void CsvFileWrite(DataRowCollection dtRows, int colCount, StreamWriter sw)
        {
            
            foreach (DataRow row in dtRows)
            {
                for (int i = 0; i < colCount; i++)
                {
                    string field = row[i].ToString();
                    sw.Write(field);
                    if (colCount - 1 > i)
                    // 最終列でない時は","を追加
                    {
                        sw.Write(",");
                    }
                }
                // 行末に改行を挿入
                sw.Write("\r\n");
            }
            sw.Close();
        }

        /// <summary>
        /// 保存したcsvファイルを読み込む。
        /// </summary>
        /// <param name="dataFile">対象csvファイル</param>
        public static DataTable CsvFileInitialRead(string dataFile)
        {
            // 初期設定
            string csvPath = Directory.GetCurrentDirectory() + "\\" + dataFile;

            DataTable tdTable = new DataTable();
            
            tdTable.Columns.Add(new DataColumn("完了", typeof(bool)));
            tdTable.Columns.Add("期限");
            tdTable.Columns.Add("やること");
            tdTable.Columns.Add(new DataColumn("表示", typeof(bool))); ;  // 表示設定（内部データ。画面上非表示にする）


            if (File.Exists(csvPath))
            // 保存済のcsvファイルが存在する場合、ファイルを開く
            {
                try
                {
                    StreamReader sr = new StreamReader(dataFile);

                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        string[] items = line.Split(',');
                        DataRow row = tdTable.NewRow();
                        tdTable.Rows.Add(items);
                    }

                    sr.Close();
                }
                catch (System.IO.IOException)
                {
                    MessageBox.Show("タスク保存ファイルが既に開いています。\r\n" +
                        "ファイルを閉じてください。", "警告",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                
            }

            return tdTable;

        }

        /// <summary>
        /// データの保存先とファイル名を指定する
        /// </summary>
        /// <returns>csvPath:データパス</returns>
        public static string CsvFileSave()
        {
            string csvPath = "";
            // 保存先とファイル名を指定する
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "TodoList.csv";
            sfd.Filter = "csvファイル(*.csv)|*.*";
            sfd.Title = "保存先を指定してください";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                csvPath = sfd.FileName;
            }
            return csvPath;
        }
    }
}
