using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;


namespace RTMPDownloader
{
	public class NetworkServer
	{
		TcpListener listener;
		TcpClient client;
		NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;
	
		public NetworkServer (){}
	
		public bool Abre(int puerto){
			try{
				Console.WriteLine ("Abriendo servidor en \"127.0.0.1:" + puerto + "\"...");
				listener = new TcpListener (IPAddress.Loopback, puerto);
				listener.Start ();
	
				Console.WriteLine ("Servidor abierto.");
				return true;
			}
			catch(Exception e){
				Console.WriteLine ("h0");
				Console.WriteLine (e);
				return false;
			}
		}
	
		public RespuestaHTTP Escucha ()
		{
	
			try{
	
				client = listener.AcceptTcpClient ();
				//Console.WriteLine ("Conexion establecida");
	
				stream = client.GetStream ();
				reader = new StreamReader (stream);
				writer = new StreamWriter (stream);
	
	
				string GETurl = reader.ReadLine ();
				
				if(GETurl != null){
					string pattern = " (.*?) HTTP";
					MatchCollection matches = Regex.Matches (GETurl, pattern);
	
					string url="";
	
					if (matches.Count > 0) {
						GroupCollection gc = matches[0].Groups;
						CaptureCollection cc = gc[1].Captures;
						url = cc[0].Value;
					}
					//Console.WriteLine (url);
	
	
					pattern = "\\?(&?([^=^&]+?)=([^&]*))*";
					matches = Regex.Matches (url, pattern);
					//Utilidades.print_r_regex(matches);
					if (matches.Count > 0) {
						GroupCollection gc = matches[0].Groups;
						CaptureCollection variables = gc[2].Captures;
						CaptureCollection valores = gc[3].Captures;
	
						ParametroGet[] parametros = new ParametroGet[variables.Count];
						for(int i = 0; i < variables.Count; i++){
							parametros[i] = new ParametroGet(
												Uri.UnescapeDataString(variables[i].Value).Replace("+", " "),
												Uri.UnescapeDataString(valores[i].Value).Replace("+", " "));
						}
						return new RespuestaHTTP (url, parametros);
					}
					return new RespuestaHTTP (url);
				}
				return new RespuestaHTTP (false);
	
			}
			catch(Exception e){
				Console.WriteLine ("h1");
				Console.WriteLine (e);
				CierraCliente ();
				return new RespuestaHTTP (false);
			}
		}
	
		public bool Envia (String que)
		{
			try {
				writer.WriteLine ("HTTP/1.1 200 OK");
				writer.WriteLine ("Connection: Close");
				writer.WriteLine ("Content-Type: text/html; charset=utf-8");
				//writer.WriteLine ("Content-Length: " + que.Length);
	
				writer.WriteLine ("");
				/*int tam = 1000;
				for(int i=0; i<que.Length; i+=tam){
					writer.Write (que.Substring(i, que.Length-i>tam?tam:que.Length-i));
				}*/
				writer.Write (que);
				writer.Flush();
	
				CierraCliente ();
				return true;
			} catch (Exception e) {
				Console.WriteLine ("h2");
				Console.WriteLine (e);
				return false;
			}
		}
		
		public bool EnviaRaw(String contentType, byte[] contenido){
			try {
				writer.WriteLine ("HTTP/1.1 200 OK");
				writer.WriteLine ("Connection: Close");
				writer.WriteLine ("Content-Type: "+contentType);
				//writer.WriteLine ("Content-Length: " + contenido.Length);
	
				writer.WriteLine ("");
				writer.Flush();
				stream.Write(contenido, 0, contenido.Length);
	
				CierraCliente ();
				return true;
			} catch (Exception e) {
				Console.WriteLine ("h10");
				Console.WriteLine (e);
				return false;
			}
		}
	
		public bool EnviaLocation (String que)
		{
			try {
				writer.WriteLine ("HTTP/1.1 301 OK");
				writer.WriteLine ("Location: " + que);
				writer.WriteLine ("Content-Length: 0");
	
				writer.WriteLine ("");
				writer.Flush();
	
				CierraCliente ();
				return true;
			} catch (Exception e) {
				Console.WriteLine("h3");
				Console.WriteLine (e);
				return false;
			}
		}
	
		public void CierraCliente ()
		{
			try{
				this.writer.Close ();
				this.stream.Close ();
				//Console.WriteLine("Cliente cerrado.");
			}
			catch(Exception e){
				Console.WriteLine("h4");
				Console.WriteLine (e);
			}
		}
	
		public void Cierra ()
		{
			try{
				CierraCliente();
				this.listener.Stop ();
				Console.WriteLine("Servidor cerrado.");
			}
			catch(Exception e){
				Console.WriteLine("h5");
				Console.WriteLine (e);
			}
		}
	}
	
	public class RespuestaHTTP
	{
		public String url;
		public String path;
		public ParametroGet[] parametros;
		bool _correcto;
		public bool correcto{
			get{
				return _correcto;
			}
			set{ }
		}
	
		public RespuestaHTTP (String url)
		{
			this._correcto = true;
			
			this.setURL(url);
		}
	
		public RespuestaHTTP (bool correcto)
		{
			this.correcto = correcto;
		}
	
		public RespuestaHTTP (String url, ParametroGet[] parametros)
		{
			this.setURL(url);
			this.parametros = parametros;
			this._correcto = true;
		}
		
		void setURL(String url){
			this.url = url;
			
			String pattern = "[^\\?]*";
			MatchCollection matches = Regex.Matches (url, pattern);
			//Utilidades.print_r_regex(matches);
	
			this.path = matches[0].Groups[0].Captures[0].Value;
		}
	
		public bool tieneParametros(){
			return parametros != null;
		}
	
		public bool existeParametro(String variable){
			if (!tieneParametros())
				return false;
	
			for(int i = 0; i < parametros.Length; i++){
				if (parametros[i].variable == variable)
					return true;
			}
			return false;
		}
	
		public String getParametro(String variable){
			if (!tieneParametros())
				return "";
	
			for (int i = 0; i < parametros.Length; i++) {
				if (parametros [i].variable == variable)
					return parametros [i].valor;
			}
			return "";
		}
	}
	
	public class ParametroGet{
		String _variable;
		String _valor;
	
		public String variable{
			get{
				return _variable;
			}
			set{ }
		}
		public String valor{
			get{
				return _valor;
			}
			set{ }
		}
	
	
		public ParametroGet (String variable, String valor)
		{
			this._variable = variable;
			this._valor = valor;
		}
	}
}
