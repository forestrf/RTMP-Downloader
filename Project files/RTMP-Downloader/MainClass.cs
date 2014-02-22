using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Net;



namespace RTMPDownloader
{
	public static class MainClass
	{
		public static string ffmpeg2file = "";
		public static string rtmpdownloaderPath = "";
		public static string relativePath = "";
	
		public static string version = "0.2b";
		
		public static int puerto = 25432;
	
		public static List<Descargador> descargasEnProceso = new List<Descargador>();
		public static int TempDescargasEnProcesoCantidad = 0;
	
	
		public static void Main (string[] cmdLine)
		{
			Console.Title = "RTMP-Downloader V" + version + " - http://www.descargavideos.TV";
			Console.WriteLine ("RTMP-Downloader V" + version + " - http://www.descargavideos.TV");
			Console.WriteLine ("");
			Debug.WriteLine(Utilidades.WL("Modo Debug"));
			Debug.WriteLine(Utilidades.WL(""));
	
			MainClass.rtmpdownloaderPath = AppDomain.CurrentDomain.BaseDirectory;
			MainClass.relativePath = Directory.GetCurrentDirectory();
	
			if (File.Exists (MainClass.rtmpdownloaderPath + "\\rtmpdump\\rtmpdump.exe")) {
				MainClass.ffmpeg2file = MainClass.rtmpdownloaderPath + "\\rtmpdump\\rtmpdump.exe";
			} else if (File.Exists (MainClass.rtmpdownloaderPath + "\\rtmpdump.exe")) {
				MainClass.ffmpeg2file = MainClass.rtmpdownloaderPath + "\\rtmpdump.exe";
			}
			if (MainClass.ffmpeg2file == "") {
				//No tsmuxer files
				Console.WriteLine ("Por favor descarga rtmpdump y coloca el archivo rtmpdump.exe dentro de la siguiente carpeta:");
				Console.WriteLine (MainClass.rtmpdownloaderPath);
				Console.WriteLine ("");
				Console.WriteLine ("Descargalo aqui:");
				Console.WriteLine ("http://rtmpdump.mplayerhq.hu/download/");
				Console.WriteLine ("");
				Console.WriteLine ("Tambien puedes encontrar el archivo en el ZIP de RTMP-Downloader");
				Console.WriteLine ("Pulsa cualquier tecla para continuar...");
				Console.ReadKey();
				return;
			}
	
	
	
			//Todo http aqui
			operaDesdeServidor ();
	
		}
	
		public static NetworkServer myServer;
	
		//Manejo desde HTTP. Todo desde aqui. Una vez terminada la funcion acabara el programa
		public static void operaDesdeServidor(){
	
			//create server and listen from port 25430
			Console.WriteLine ("Para usar el programa abre en un navegador la siguiente URL:");
			Console.WriteLine ("http://127.0.0.1:"+puerto+"/");
			Console.WriteLine ("");
	
	
			myServer = new NetworkServer ();
	
			if (!myServer.Abre (puerto)) {
				Console.WriteLine ("No se ha podido abrir servidor.");
				Console.WriteLine ("Es posible que ya tengas el programa abierto.");
				Console.WriteLine ("");
	
				return;
			}
			
			//Abre navegador
			Process.Start("http://127.0.0.1:"+puerto+"/");
			
			while(true) {
				RespuestaHTTP GETurl = myServer.Escucha();
	
				if (!GETurl.correcto) {
					myServer.CierraCliente ();
					continue;
				}
	
				string path = GETurl.path;
	
				string accion = GETurl.getParametro ("accion");
	
				string nombre = GETurl.getParametro ("nombre");
				string url = GETurl.getParametro ("url");
				
				string urlhttp = GETurl.getParametro ("urlhttp");
	
				if (accion == "" || accion == "descargar") {
					Debug.WriteLine(Utilidades.WL("descargar"));
					if (url != "") {
						Debug.WriteLine(Utilidades.WL("url"));
						string cerrarVentana = GETurl.getParametro ("cerrarVentana");
						if(cerrarVentana == "" || cerrarVentana == "1"){
							myServer.Envia(HTML.cierraConJS());
						}
						//else if(cerrarVentana == "0" || true){
						else{
							myServer.EnviaLocation ("/");
						}
						var t = new Thread(() => lanzaDescarga(url, nombre));
						t.Start();
						continue;
					}
					else if (urlhttp != "") {
						Debug.WriteLine(Utilidades.WL("urlhttp"));
						string cerrarVentana = GETurl.getParametro ("cerrarVentana");
						if(cerrarVentana == "" || cerrarVentana == "1"){
							myServer.Envia(HTML.cierraConJS());
						}
						//else if(cerrarVentana == "0" || true){
						else{
							myServer.EnviaLocation ("/");
						}
						//Descargar urlhttp para usar el contenido como url
						Debug.WriteLine(Utilidades.WL("urlhttp=>"+urlhttp));
						url = new WebClient().DownloadString(urlhttp);
						Debug.WriteLine(Utilidades.WL("url=>"+url));
						var t = new Thread(() => lanzaDescarga(url, nombre));
						t.Start();
						continue;
					}
				}
	
				if(path == "/ayuda"){
					myServer.Envia (HTML.getAyuda());
					continue;
				}
				
				if(path == "/ayuda/ayuda_prev.png"){
					byte[] imgBytes = GetILocalFileBytes.Get("RTMPDownloader.ayuda_img.png");
					myServer.EnviaRaw ("image/png", imgBytes);
					continue;
				}
				
				if(path == "/all.css"){
					myServer.Envia (HTML.getAllcss());
					continue;
				}
	
				if(path == "/rtmpdownloader.js"){
					myServer.Envia (HTML.getRTMPdownloaderjs());
					continue;
				}
			
				if (path == "/" && accion == "") {
					myServer.Envia (HTML.getIndex());
					continue;
				}
	
				if (accion == "progreso") {
					//Mostrar un alert en caso de que se agregue una nueva descarga para conseguir el focus de la pestaña
					if(descargasEnProceso.Count > TempDescargasEnProcesoCantidad){
						myServer.Envia (HTML.getProgreso("Descarga agregada"));
						TempDescargasEnProcesoCantidad = descargasEnProceso.Count;
					}
					else{
						myServer.Envia (HTML.getProgreso());
					}
					continue;
				}
				
				if (accion == "cancelarDescarga") {
					int elem = Convert.ToInt32(GETurl.getParametro ("elem"));
					if(elem >= 0 && elem < descargasEnProceso.Count){
						borraDescarga(descargasEnProceso[elem]);
					}
					myServer.EnviaLocation("/");
					continue;
				}
				
				if (accion == "cerrarPrograma") {
					for(int i=0; i< descargasEnProceso.Count; i++){
						descargasEnProceso[i].Cancelar();
					}
					myServer.Envia(HTML.getCerrado());
					
					myServer.Cierra();
					
					return;
				}
	
	
				myServer.Envia ("Na que hacer");
			}
		}
	
		public static void borraDescarga(Descargador cual){
			cual.Cancelar();
			descargasEnProceso.Remove(cual);
			
			TempDescargasEnProcesoCantidad = descargasEnProceso.Count;
		}
	
		public static void lanzaDescarga(string url, string nombre){
			
			Descargador miDescargador = new Descargador ();
	
			if (nombre == "") {
				nombre = Utilidades.GetParametro (url, "o");
			}
	
			if (nombre == "") {
				nombre = "video.mp4";
			}
			for (int j=1; File.Exists(nombre); j++) {
				nombre = "video" + j + ".mp4";
			}
	
			if (miDescargador.Comienza (url, nombre)) {
				//Abrir carpeta que tiene el video
				//string myDocspath = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
				string windir = Environment.GetEnvironmentVariable ("WINDIR");
				System.Diagnostics.Process prc = new System.Diagnostics.Process ();
				prc.StartInfo.FileName = windir + @"\explorer.exe";
				prc.StartInfo.Arguments = MainClass.relativePath;
				prc.Start ();
			}
			
		}
	}
}