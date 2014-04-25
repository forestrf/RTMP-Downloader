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
		public static string rtmpdumpFile = "";
		public static string relativePath = "";
	
		public static string version = "0.3";
		
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

			MainClass.relativePath = AppDomain.CurrentDomain.BaseDirectory;
	
			if (File.Exists (MainClass.relativePath + "rtmpdump\\rtmpdump.exe")) {
				MainClass.rtmpdumpFile = MainClass.relativePath + "rtmpdump\\rtmpdump.exe";
				Debug.WriteLine(Utilidades.WL("RTMPDUMP encontrado en: "+MainClass.relativePath + "\\rtmpdump\\rtmpdump.exe"));
			} else if (File.Exists (MainClass.relativePath + "rtmpdump.exe")) {
				MainClass.rtmpdumpFile = MainClass.relativePath + "rtmpdump.exe";
				Debug.WriteLine(Utilidades.WL("RTMPDUMP encontrado en: "+MainClass.relativePath + "rtmpdump.exe"));
			}
			if (MainClass.rtmpdumpFile == "") {
				//No se encuentra el archivo necesario
				Debug.WriteLine(Utilidades.WL("RTMPDUMP NO encontrado"));

				Console.WriteLine ("Por favor descarga rtmpdump y coloca el archivo rtmpdump.exe dentro de la siguiente carpeta:");
				Console.WriteLine (MainClass.relativePath);
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

			Debug.WriteLine(Utilidades.WL("Arrancando servidor"));
	
	
			myServer = new NetworkServer ();
	
			if (!myServer.Abre (puerto)) {
				Console.WriteLine ("No se ha podido abrir servidor.");
				Console.WriteLine ("Es posible que ya tengas el programa abierto.");
				Console.WriteLine ("");
				
				Debug.WriteLine(Utilidades.WL("No se ha podido arrancar el servidor"));

				return;
			}

			Debug.WriteLine(Utilidades.WL("Servidor arrancado"));
			
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

				if (accion != "progreso") {
					Debug.WriteLine(Utilidades.WL(""));
					Debug.WriteLine (Utilidades.WL ("------------------------------------------------------------"));
					Debug.WriteLine (Utilidades.WL ("Nueva petición realizada al servidor:"));
					Debug.WriteLine (Utilidades.WL ("path = " + path));
					Debug.WriteLine (Utilidades.WL ("accion = " + accion));
					Debug.WriteLine (Utilidades.WL ("nombre = " + nombre));
					Debug.WriteLine (Utilidades.WL ("url = " + url));
					Debug.WriteLine (Utilidades.WL ("urlhttp = " + urlhttp));
					Debug.WriteLine (Utilidades.WL ("------------------------------------------------------------"));
					Debug.WriteLine(Utilidades.WL(""));
				}

				if (accion == "" || accion == "descargar") {
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
						Debug.WriteLine(Utilidades.WL("Poniendo descarga en cola..."));
						var t = new Thread(() => lanzaDescarga(url, nombre));
						t.Start();
						Debug.WriteLine(Utilidades.WL("Descarga puesta en cola"));
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
						try{
							url = new WebClient().DownloadString(urlhttp);
							Debug.WriteLine(Utilidades.WL("url descargada desde urlhttp = "+url));
							Debug.WriteLine(Utilidades.WL("Poniendo la descarga en cola"));
							var t = new Thread(() => lanzaDescarga(url, nombre));
							t.Start();
							Debug.WriteLine(Utilidades.WL("Descarga puesta en cola"));
						}
						catch(Exception e){
							Console.WriteLine (e);
							Debug.WriteLine(Utilidades.WL(e.ToString()));
						}
						continue;
					}
				}
	
				if(path == "/ayuda"){
					myServer.Envia (HTML.getAyuda());
					continue;
				}

				if(path == "/opciones"){
					myServer.Envia (HTML.getOpciones());
					continue;
				}
				
				if(path == "/ayuda/ayuda_prev.png"){
					byte[] imgBytes = GetILocalFileBytes.Get("RTMPDownloader.ayuda_img.png");
					myServer.EnviaRaw ("image/png", imgBytes);
					continue;
				}
				
				if(path == "/all.css"){
					byte[] cssBytes = GetILocalFileBytes.Get("RTMPDownloader.web.all.css");
					myServer.EnviaRaw ("text/css; charset=utf-8", cssBytes);
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
	
	
				myServer.Envia ("No se encuentra la orden.");
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