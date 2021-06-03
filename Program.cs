using Dropbox.Api;
using Dropbox.Api.Files;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DropBox
{
    class Program
    {
        public string token
        {
            get
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings["token"];
            }
        }

        public string pathLocal
        {
            get
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings["path"];
            }
        }

        public string log { get; set; }

        static void Main(string[] args)
        {
            var task = Task.Run((Func<Task>)Program.Run);
            task.Wait();
        }

        void GravarLog(string log)
        {
            try
            {
                if (string.IsNullOrEmpty(log)) return;

                using (StreamWriter sw =
                    new StreamWriter(string.Format("{0}/log_{1}.log",
                    this.pathLocal, DateTime.Now.ToString("yyyyMMdd")), true))
                {
                    sw.WriteLine(log);
                    sw.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }
        }

        static async Task Run()
        {
            Program p = new Program();
            using (var dbx = new DropboxClient(p.token))
            {
                try
                {
                    p.log += string.Format("INICIO VERIFICAÇÃO: {0}\n", DateTime.Now.ToString("yyyy-MM-dd"));
                    if (!Directory.Exists(p.pathLocal))
                        return;

                    var full = await dbx.Users.GetCurrentAccountAsync();
                    p.log += string.Format("DROPBOX CONTA: {0} - {1}\n", full.Name.DisplayName, full.Email);

                    //string[] diretorios = Directory.GetDirectories("C:\\");
                    string[] arquivos = Directory.GetFiles(p.pathLocal);

                    p.log += string.Format("QUANTIDADE DE ARQUIVOS A SEREM ENVIADOS: {0}\n", arquivos.Length);
                    foreach (string arquivo in arquivos)
                    {
                        await p.Upload(dbx, "/Anexo", Path.GetFileName(arquivo), arquivo);
                        p.log += string.Format("ENVIADO ARQUIVO: {0}\n", arquivo);                        
                        //await p.Upload(dbx, "/Anexo", "arquivo.pdf", "D:/temp/arquivo.pdf");
                    }
                    //await p.Download(dbx, "/Anexo", "arquivo.pdf");
                    //await p.ListRootFolder(dbx);  

                    p.log += string.Format("FIM VERIFICAÇÃO: {0}\n", DateTime.Now.ToString("yyyy-MM-dd"));
                    p.GravarLog(p.log);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    p.log += string.Format("{0}\n", ex.Message);
                    p.GravarLog(p.log);
                }
            }
        }

        async Task ListRootFolder(DropboxClient dbx)
        {
            var list = await dbx.Files.ListFolderAsync(string.Empty);

            // show folders then files
            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                Console.WriteLine("D  {0}/", item.Name);
            }

            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                Console.WriteLine("F{0,8} {1}", item.AsFile.Size, item.Name);
            }
        }

        async Task Download(DropboxClient dbx, string folder, string file)
        {
            using (var response = await dbx.Files.DownloadAsync(string.Format("{0}/{1}", folder, file)))
            {
                using (FileStream fs = new FileStream(string.Format("{0}/{1}", pathLocal, file), FileMode.Create))
                {
                    byte[] Buffer = await response.GetContentAsByteArrayAsync();
                    fs.Write(Buffer, 0, Buffer.Length);
                    fs.Close();
                }
            }
        }

        async Task Upload(DropboxClient dbx, string folder, string file, string content)
        {
            using (var mem = new FileStream(content, FileMode.Open))
            {
                var updated = await dbx.Files.UploadAsync(
                    folder + "/" + file,
                    WriteMode.Overwrite.Instance,
                    body: mem);                
            }
        }

    }
}
