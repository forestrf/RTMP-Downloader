using System;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace RTMPDownloader
{
	public class Descargador
	{
		public string url = "a";
		public string nombre = "b";
	
		public double tiempoTotal = -1;
		public double tiempoActual = -1;
		public double horaInicio = -1;
	
		public int porcentajeInt = 0;
		public double porcentaje = 0;
		public string horaRestanteString = "";
		
		Boolean cancelado = false;
		public String fallado = "";
		
		ProcessStartInfo procesoFFMPEG2;
		Process exeProcessProcesoFFMPEG2;
	
		
		public bool Comienza (string url, string nombre)
		{
			this.url = url;
			this.nombre = nombre;
	
			return download ();
		}
	
		public void Cancelar ()
		{
			if(exeProcessProcesoFFMPEG2!=null)
				if(!exeProcessProcesoFFMPEG2.HasExited)
					exeProcessProcesoFFMPEG2.Kill();
			cancelado = true;
		}
	
		public bool download ()
		{
	
			MainClass.descargasEnProceso.Add (this);
	
			try {
				Console.WriteLine ("Descargando. Espere por favor...");
	
				if(nombre != "")
					url = Utilidades.ReemplazaParametro (url, "o", nombre);
	
				procesoFFMPEG2 = new ProcessStartInfo ();
				procesoFFMPEG2.FileName = MainClass.ffmpeg2file;
				procesoFFMPEG2.Arguments = url;
	
				procesoFFMPEG2.UseShellExecute = false;
				procesoFFMPEG2.RedirectStandardOutput = true;
				procesoFFMPEG2.RedirectStandardError = true;
				procesoFFMPEG2.CreateNoWindow = true;
	
	
				exeProcessProcesoFFMPEG2 = Process.Start (procesoFFMPEG2);
	
				exeProcessProcesoFFMPEG2.OutputDataReceived += p_OutputDataReceived;
				exeProcessProcesoFFMPEG2.ErrorDataReceived += p_ErrorDataReceived;
	
				exeProcessProcesoFFMPEG2.BeginOutputReadLine();
				exeProcessProcesoFFMPEG2.BeginErrorReadLine();
	
				exeProcessProcesoFFMPEG2.WaitForExit ();
			} catch (Exception e) {
				//En caso de que FFmpeg falle no siempre dara excepcion (por ejemplo, cuando es necesario cambiar de proxy el server los enlaces no funcionan bien, pero no se activa este fallo)
				Console.WriteLine ("RTMPDump ha fallado.");
				fallado = "RTMPDump ha fallado";
				return false;
			}
	
			//if(!cancelado)
			//	return true;
			//else
			//	return true;
	
			if (porcentajeInt == 0)
				fallado = "Fallo";
			else {
				porcentaje = 100;
				porcentajeInt = 100;
			}
	
			
			return !cancelado;
		}
	
		public void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			//FFMPEG NO USA ESTO
			System.Diagnostics.Debug.WriteLine("Received from standard out: " + e.Data);
		}
	
		public void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine("Received from standard error: " + e.Data);
			//System.Diagnostics.Debug.WriteLine(e.Data);
			if (!String.IsNullOrEmpty(e.Data)) {
				if (e.Data.IndexOf ("kB / ") > 0) {
					if (tiempoTotal == -1) {
						tiempoTotal = 100;
	
						horaInicio = Utilidades.UnixTimestamp();
					} else {
						//Console.WriteLine(e.Data);
	
						int inicio = e.Data.IndexOf ("(") + 1;
						int final = e.Data.IndexOf ("%", inicio);
						//0.783 kB / 0.00 sec (0.0%)
						string tiempo = e.Data.Substring (inicio, final - inicio);
	
						tiempoActual = double.Parse (tiempo, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
	
						System.Diagnostics.Debug.WriteLine (tiempo);
						System.Diagnostics.Debug.WriteLine (tiempoActual);
	
						porcentaje = Math.Round (tiempoActual / tiempoTotal * 100.0,
													2, MidpointRounding.AwayFromZero);
	
						porcentajeInt = (int)porcentaje;
	
						int horaActual = Utilidades.UnixTimestamp();
	
						int horaTranscurrida = (int)horaActual - (int)horaInicio;
						int horaRestante = (int)((horaTranscurrida/porcentaje)*(100-porcentaje));
	
						horaRestanteString = segundosATiempo (horaRestante);
	
	
	
						Console.WriteLine (nombre + " - " + porcentajeInt + "%" + " - Quedan: " + horaRestanteString);
	
					}
				}
			}
		}
	
		public string segundosATiempo(int seg_ini) {
			int horas = (int)Math.Floor((double)(seg_ini/3600));
			int minutos = (int)Math.Floor((double)((seg_ini-(horas*3600))/60));
			int segundos = seg_ini-(horas*3600)-(minutos*60);
			if(horas > 0)
				return horas+" horas, "+minutos+"min, "+segundos+"seg";
			if(minutos > 0)
				return minutos+" min, "+segundos+" seg";
			return segundos+" seg";
		}
		
	}
}

