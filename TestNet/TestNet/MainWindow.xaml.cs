using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace TestNet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NetworkStream stream;
        System.Net.Sockets.TcpClient client;
        FlowDocument mydoc;
        Paragraph myParagraph;
        TextPointer FindPosition;
        int FindIndex;
        BackgroundWorker worker;
        BackgroundWorker bw;
        class AsyncReadParams
        {
            public int counter;
            public string Mode;
        };

        public MainWindow()
        {
            InitializeComponent();
            LogRTB.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            mydoc = new FlowDocument();
            LogRTB.Document = mydoc;
            myParagraph = new Paragraph();
            FindIndex = 0;
            string CurrentDate = DateTime.Now.ToString("yyyy-MM-dd H:mm:ss");
            TimeTB.Items.Add(CurrentDate);
            TimeTB.SelectedItem = TimeTB.Items[0];
            AfterConnectSP.Visibility = Visibility.Collapsed;
        }


        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            int errflag = 0;
            client = new System.Net.Sockets.TcpClient();
           
            try{
                client.Connect(HostTB.Text, 2743);
                stream = client.GetStream();
                stream.ReadTimeout = 20000;
            }
           catch(Exception error)
               {
                   MessageBox.Show(error.Message);
                   errflag = 1;
               }  
           
           if(errflag<1)
           {
               MessageBox.Show("Connect");
               AfterConnectSP.Visibility = Visibility.Visible;
           }
            
         
        }

        private void Ping_Click(object sender, RoutedEventArgs e)
        {
          


            byte[] data =
                System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"Ping\"}");
            stream.Write(data, 0, data.Length);
            data = new Byte[256];
            int n = stream.Read(data, 0, 256);
            string answer = System.Text.Encoding.Default.GetString(data, 0, n);
            if (answer == "PONG")
                MessageBox.Show("Ping OK");
          
        }
        
        void AsrRead(string Mode)
        {
            byte[] senddata =
             System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"Read\",\"LineCount\":\"" + LineCountTB.Text + "\",\"Mode\":\""+Mode+"\"}");
            stream.Write(senddata, 0, senddata.Length);
        }

        private void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
            string Mode =  "Safe";
            if(!ReadModeChKB.IsChecked==false)
                Mode =  "UnSafe";

            AsrRead(Mode);
            int counter = Convert.ToInt32(LineCountTB.Text);
          
            ReadLines(counter,Convert.ToInt32(Mode=="Safe"));
            FindPosition = LogRTB.Document.ContentStart;
        }
   
      
        private void SetLogPathBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] data =
                   System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"SetLogPath\",\"LogPath\":\"" + LogPathTB.Text + "\"}");
                stream.Write(data, 0, data.Length);
                data = new Byte[256];
                int n = stream.Read(data, 0, 256);
                string answer = System.Text.Encoding.Default.GetString(data, 0, n);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void SetPositionBtn_Click(object sender, RoutedEventArgs e)
        {
            try {
                TimeTB.Items.Add(TimeTB.Text);
                byte[] data;
                if(LogNameTB.SelectedIndex==0)
                    data = System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"Find\",\"Filename\":\"" + LogNameTB.Text + "\",\"AskTime\":\"" + TimeTB.Text + "\"}");
                else
                {
                    data = System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"Find\",\"Filename\":\"" + LogNameTB.Text + "\",\"AskTime\":\"" + TimeTB.Text + "\",\"GZIP\":\"1\"}");
                
                }
                stream.Write(data, 0, data.Length);
                data = new Byte[256];
                int n = stream.Read(data, 0, 256);
                string answer = System.Text.Encoding.Default.GetString(data, 0, n);
                if (answer == "ERROR")
                    MessageBox.Show("Not Found");
            }
            catch(Exception error)
            {
                MessageBox.Show(error.Message);
            }
           
        }

        private void NextCallBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                byte[] data =
                    System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"NextCall\"}");
                stream.Write(data, 0, data.Length);
                data = new Byte[256];
                int n = stream.Read(data, 0, 256);
                string answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);

                myParagraph.Inlines.Add(answer + '\n');
                mydoc.Blocks.Add(myParagraph);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            myParagraph = new Paragraph();
            mydoc.Blocks.Clear();
           // AfterConnectSP.Visibility = Visibility.Collapsed;
        }

        private void AsyncReadBtn_Click(object sender, RoutedEventArgs e)
        {

            AsyncReadParams param = new AsyncReadParams();
            param.counter = Convert.ToInt32(LineCountTB.Text);
            if (ReadModeChKB.IsChecked==true)
                param.Mode = "Safe";
            else
                param.Mode = "UnSafe";

            AsrRead(param.Mode);

            LoadPb.Maximum = Convert.ToInt32(LineCountTB.Text);
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += Progress_Handle;
            worker.DoWork += DoWork_Handle;
            worker.RunWorkerCompleted += Complete_Handle;
            worker.WorkerSupportsCancellation = true;

            worker.RunWorkerAsync(param);
        }

        void DoWork_Handle(object sender, DoWorkEventArgs e)
        {
            int Mode = 0;
            List<string> logList = new List<string>();
            AsyncReadParams param = (AsyncReadParams)e.Argument;
            int counter = param.counter;
            if (param.Mode == "Safe")
                Mode = 1;
            byte[] data = new Byte[5256];
            byte[] senddata;
            char[] trim = {'\r','\n'};
            for (int i = 1; i < counter + 1; ++i)
            {
                BackgroundWorker bgw = sender as BackgroundWorker;
                if (bgw.CancellationPending == true)
                {
                    e.Cancel = true;
                    senddata = System.Text.Encoding.ASCII.GetBytes("CANCEL");
                    stream.Write(senddata, 0, senddata.Length);
                    stream.Read(data, 0, 1024);
                    break;
                }
                int n = 0;
                try {
                        n = stream.Read(data, 0, 1256);
                    }
                catch(Exception error)
                {
                    MessageBox.Show(error.Message);
                    MessageBox.Show("Всего прочитано "+i.ToString()+" строк");
                    break;
                }
                 
               
                string answer;
                if (data[0] == 255)
                    answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);
                else
                {
                    answer = System.Text.Encoding.Default.GetString(data, 0, n);
                }

                (sender as BackgroundWorker).ReportProgress(i);
                logList.Add(answer.TrimEnd(trim));
                    //Add();
                if (Mode == 1)
                {
                    senddata = System.Text.Encoding.ASCII.GetBytes("{\"Receive\":\"" + n.ToString() + "\"}");
                    stream.Write(senddata, 0, senddata.Length);
                }

            }
            e.Result = logList;
            
        }
        void Progress_Handle(object sender, ProgressChangedEventArgs e)
        {
            LoadPb.Value = e.ProgressPercentage;
        }
        void Complete_Handle(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                List<string> LogList = (List<string>)e.Result;
                foreach (string answer in LogList)
                    //    myParagraph.Inlines.Add(answer);
                    LogRTB.AppendText(answer + '\r');
                mydoc.Blocks.Add(myParagraph);
            }
            LoadPb.Value = 0;
            FindPosition = LogRTB.Document.ContentStart;
        }

        private void FindBtn_Click(object sender, RoutedEventArgs e)
        {
            FindTB.Items.Add(FindTB.Text);
            try
            {
                if (LogRTB.Selection.Start != LogRTB.Selection.End)
                    FindPosition = LogRTB.Selection.End;

                TextRange test = new TextRange(FindPosition, LogRTB.Document.ContentEnd);
                string teststr = test.Text;
                int index = teststr.IndexOf(FindTB.Text) + FindIndex;

                if (index >= 0)
                {
                    TextPointer start = FindPosition.GetPositionAtOffset(index + 2);
                    TextPointer end = FindPosition.GetPositionAtOffset(index + FindTB.Text.Length + 2);
                    TextRange tr = new TextRange(start, end);
                    tr.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Orange);
                    tr.Select(start, end);
                    // tr.Select();
                    LogRTB.Selection.Select(tr.Start, tr.Start.GetPositionAtOffset(FindTB.Text.Length));
                    Rect findoffset = tr.Start.GetCharacterRect(LogicalDirection.Forward);
                    double offset = findoffset.Top + LogRTB.VerticalOffset;
                    LogRTB.ScrollToVerticalOffset(offset - LogRTB.ActualHeight / 2);
                    FindPosition = end;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
         
        }

        private void FindTextBtn_Click(object sender, RoutedEventArgs e)
        {
            FindTextTB.Items.Add(FindTextTB.Text);
            try
            {
                byte[] data =
                   System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"FindText\",\"SearchFor\":\"" + FindTextTB.Text + "\"}");
                stream.Write(data, 0, data.Length);
                data = new Byte[8096];

                bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                LoadPb.Value = 0;
                LoadPb.Maximum = 100;
                bw.ProgressChanged += Progress_Handle;
               
                bw.DoWork += FindText_Handle;
                bw.RunWorkerCompleted += FindTextComplete_Handle;
                bw.WorkerSupportsCancellation = true;
                bw.RunWorkerAsync();
             
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }
        void FindTextComplete_Handle(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            { 
                MessageBox.Show("Поиск был отменён пользователем");
                return;
            }
               
            if (Convert.ToInt32(e.Result) == 1)
            {
                byte[] data = new Byte[8096];
                int n = stream.Read(data, 0, 8096);
                string answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);
                LoadPb.Value = 100;
                myParagraph.Inlines.Add(answer + '\n');
                mydoc.Blocks.Add(myParagraph);
            }
            else
                MessageBox.Show("Строка не найдена");
           
        }
        void FindText_Handle(object sender,DoWorkEventArgs e)
        {
            try
            {
                byte[] data = new Byte[1024];
                byte[] senddata = new Byte[128];
                int n = 1;
                do
                {

                    n = stream.Read(data, 0, 1024);
                    if (n <= 0)
                        continue;

                    BackgroundWorker bgw = sender as BackgroundWorker;
                    if (bgw.CancellationPending == true)
                    {
                        e.Cancel = true;
                        senddata = System.Text.Encoding.ASCII.GetBytes("CANCEL");
                        stream.Write(senddata, 0, senddata.Length);
                        n = stream.Read(data, 0, 1024);
                        break;
                    }
                    string answer;
                    answer = System.Text.Encoding.Default.GetString(data, 0, n);
                    if (answer.StartsWith("ERROR"))
                    {
                        e.Result = 0;
                        senddata = System.Text.Encoding.ASCII.GetBytes("{\"Receive\":\"" + n.ToString() + "\"}");
                        stream.Write(senddata, 0, senddata.Length);
                        return;
                    }
                    else if (answer.StartsWith("PROGRESS"))
                    {
                        string[] prg = answer.Split(':');
                        (sender as BackgroundWorker).ReportProgress(Convert.ToInt32(prg[1]));
                        senddata = System.Text.Encoding.ASCII.GetBytes("PROGRESS");
                        stream.Write(senddata, 0, senddata.Length);
                    }
                    else if (answer.StartsWith("FINDTEXTOK"))
                    {
                        e.Result = 1;
                        senddata = System.Text.Encoding.ASCII.GetBytes("{\"Receive\":\"" + n.ToString() + "\"}");
                        stream.Write(senddata, 0, senddata.Length);
                        return;
                    }



                } while (n > 0);
            }
            catch (Exception ex)
            {
                e.Result = 0;
                MessageBox.Show(ex.Message);
            }
        }
        private void ReadLines(int counter,int Mode)
        {
            byte[] senddata = new byte[8096];
            counter = Convert.ToInt32(LineCountTB.Text);
            // Paragraph myParagraph = new Paragraph();

            byte[] data = new Byte[5256];

            for (int i = 1; i < counter + 1; ++i)
            {
                int n = stream.Read(data, 0, 1256);
                
                string answer;
                if (data[0] == 255)
                    answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);
                else
                {
                    answer = System.Text.Encoding.Default.GetString(data, 0, n);
                }
                if (answer.StartsWith("ERROR"))
                    break;
                myParagraph.Inlines.Add(answer + '\n');

                if(Mode==1)
                {
                    senddata = System.Text.Encoding.ASCII.GetBytes("{\"Receive\":\"" + n.ToString() + "\"}");
                    stream.Write(senddata, 0, senddata.Length);
                }
               


            }

            mydoc.Blocks.Add(myParagraph);
        }
        private void ReadBackBtn_Click(object sender, RoutedEventArgs e)
        {
            try { 
                    byte[] senddata =
                      System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"ReadBack\",\"LineCount\":\"" + ReadBackTB.Text + "\"}");
                    stream.Write(senddata, 0, senddata.Length);

                    byte[] data = new byte[8096];
                    int n = stream.Read(data, 0, 8096);
                    string answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);

                    myParagraph.Inlines.Add(answer + '\n');
                    mydoc.Blocks.Add(myParagraph);

              
                    FindPosition = LogRTB.Document.ContentStart;
                }
            catch(Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void ColoredBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = 1;
                string teststr;
                TextRange test;
                while (index >= 0)
                {
                    test = new TextRange(FindPosition, LogRTB.Document.ContentEnd);
                    teststr = test.Text;
                    index = teststr.IndexOf(FindTB.Text) + FindIndex;


                    TextPointer start = FindPosition.GetPositionAtOffset(index + 2);
                    TextPointer end = FindPosition.GetPositionAtOffset(index + FindTB.Text.Length + 2);
                    TextRange tr = new TextRange(start, end);
                    tr.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Red);

                    FindPosition = end;
                }
                FindPosition = LogRTB.Document.ContentStart;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            stream.Close();
        }

        private void StopFindTextBtn_Click(object sender, RoutedEventArgs e)
        {
            bw.CancelAsync();
        }

        private void StopReadBtn_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
        }

        private void ReadLog(string EndPhrase)
        {
            while (true)
            {
                byte[] data = new byte[8096];
                byte[] senddata = new byte[128];
                string answer;
                int n = stream.Read(data, 0, 8096);
                if (data[0] == 0xff)
                    answer = System.Text.Encoding.Default.GetString(data, 1, n - 1);
                else
                    answer = System.Text.Encoding.Default.GetString(data, 0, n);
                if (answer.StartsWith(EndPhrase))
                {
                    myParagraph.Inlines.Add("SUCCESS READ LOG\n");
                    mydoc.Blocks.Add(myParagraph);
                    break;
                }
                if (answer.StartsWith("ERROR"))
                {
                    myParagraph.Inlines.Add("ERROR WHILE READ LOG\n");
                    mydoc.Blocks.Add(myParagraph);

                    break;
                }
                senddata = System.Text.Encoding.ASCII.GetBytes("{\"Receive\":\"" + n.ToString() + "\"}");
                stream.Write(senddata, 0, senddata.Length);

                myParagraph.Inlines.Add(answer + '\n');
                mydoc.Blocks.Add(myParagraph);


            }

        }
        private void ReadSIPBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReadSIPTB.Items.Add(ReadSIPTB.Text);
                byte[] senddata =
                     System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"ReadCallSIP\",\"CallID\":\"" + ReadSIPTB.Text + "\"}");
                stream.Write(senddata, 0, senddata.Length);
                ReadLog("ENDSIP");
               
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void ReadCallBtn_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                ReadCallTB.Items.Add(ReadCallTB.Text);
                byte[] senddata =
                     System.Text.Encoding.ASCII.GetBytes("{\"Action\":\"ReadCallLog\",\"CallID\":\"" + ReadCallTB.Text + "\"}");
                stream.Write(senddata, 0, senddata.Length);
                ReadLog("ENDLOG");

            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

      
    }
}
