using System;
using System.Runtime.InteropServices;
using CA200SRVRLib;

namespace SONY.Modules
{
	public class CaSdk
	{
		//Additional error code
		const Int32 NotPowerOn = -1;
		const Int32 FileCheckError = -2;
		const Int32 OtherError = -3;

		/// <summary>DisplayMode</summary>
		public enum DisplayMode
		{
			DispModeLvxy, DispModeTdudv, DispModeANL, DispModeANLG, DispModeANLR,
			DispModePUV, DispModeFMA, DispModeXYZ, DispModeJEITA
		};
		/// <summary>SyncMode</summary>
		/// <remarks>Now this enum is not use.Because SyncMode is float</remarks>
		public enum SyncMode { SyncNTSC, SyncPAL, SyncEXT, SyncUNIV };
		/// <summary>RemoteMode</summary>
		public enum RemoteMode { RemoteOff, RemoteOn, RemoteLock };
		/// <summary>>SetLvxyCalData lClr</summary>
		public enum ColorNo { ColorRed, ColorGreen, ColorBlue, ColorWhite };
		/// <summary>DisplayDigits</summary>
		public enum DisplayDigits { DisplayDigit3, DisplayDigit4 };
		/// <summary>>AveragingMode</summary>
		public enum AveragingMode { AveragingSlow, AveragingFast, AveragingAuto };
		/// <summary>BrightnessUnit</summary>
		public enum BrightnessUnit { BrightUnitfL, BrightUnitcdm2 };
		/// <summary>CalStandard</summary>
		public enum CalStandard{Cal6500K = 1,Cal9300K};

		String errorParam = "";
		Int32 errorNo = 0;
		bool[] useProbe = new bool[5];
		DisplayMode nowDisplayMode = DisplayMode.DispModeLvxy;
		Int32 ca210ID = 0;
		Ca200 Ca210 = null;
		Cas cas = null;
		Ca ca = null;
		OutputProbes outputProbes = null;
		Memory memory = null;
		Probes probes = null;
		Probe[] probe = null;

		/// <summary>Is opend CA-SDK.</summary>
		/// <returns>true=opened/false=not opened</returns>
		public bool isOpened() {
			if (ca == null)
				return false;
			return true;
		}

		bool reopenRequired = false;
		/// <summary>Get Reopen Required flag.</summary>
		/// <returns>true=Necessar to reopen/false=Not necessar to reopen</returns>
		public bool getReopenRequired() {
			return reopenRequired;
		}

		/// <summary>Get errormessage</summary>
		/// <remarks>
		/// Almost all function return result.
		/// if result is false,use this function for get error message.
		/// </remarks>
		/// <returns>ERROR MESSAGE</returns>
		public String getErrorMessage() {
			reopenRequired = false;
			if (errorNo == 0)
				return errorParam;
			switch (errorNo) {
				case NotPowerOn:
					reopenRequired = true;
					return ("Power On And Program Restart.");
				case FileCheckError:
					return ("File check error");
				case OtherError:
					return errorParam;
			}
			switch (errorNo & 0xffff) {
				case 1:
					return "PROBE SERIAL ERROR";
				case 2:
					return "TEMP ERROR";
				case 3:
					return "PROBE SERIAL && TEMP ERROR";
				case 4:
					return "OUT OF RANGE ERROR";
				case 5:
					return "PROBE SERIAL && OUT OF RANGE ERROR";
				case 6:
					return "TEMP && OUT OF RANGE ERROR";
				case 7:
					return "PROBE SERIAL && TEMP && OUT OF RANGE ERROR";
				case 10:
					return "Measurement was executed before zero calibration\n or Remote Off.";
				case 15:
					return "Occurrence of hold error.";
				case 20:
					return "An invalied external synchronized signal.";
				case 22:
					return "Over the measurement range.";
				case 23:
					return "Offset error.";
				case 50:
					return "Measurement value is over 100% in the flicker mode.";
				case 51:
					return "External synchronizing signal is over 130Hz in the FMA flicker mode.";
				case 52:
					return "Measurement value in the flicker mode is too dark.";
				case 53:
					return "This probe can't measure by flicker mode.";
				case 401:
					return "Cannot execute now.";
				case 402:
					return "Invalid argument.";
				case 403:
					return "Dupulication of name ot ID,etc.";
				case 405:
					reopenRequired = true;
					return "API Fail.Restart Program.";
				case 406:
					return "Cannot open cal_data_file.\nCheck fileName.";
				case 407:
					return "Invalid CA number.";
				case 408:
					return "Invalid RS/USB ID.";
				case 409:
					return "Invalid Baurate.";
				case 410:
					return "Null pointer.";
				case 411:
					return "Probe1 shoud be connect.";
				case 412:
					return "Probe not connected.";
				case 413:
					return "Invalid x/y.";
				case 414:
					return "Invalud Lv.";
				case 415:
					return "Invalid color.";
				case 416:
					return "Invalid Index.";
				case 417:
					return "Invalid CA ID";
				case 418:
					return "Invalid Memory Channel number.";
				case 419:
					return "Invalid Memory Channel ID.";
				case 420:
					return "ID is too long(>10).";
				case 421:
					return "Invalid probe number.";
				case 422:
					return "Invalid probe ID.";
				case 423:
					return "Data value is too low.";
				case 424:
					return "Data value is too high.";
				case 425:
					return "Nonexistant object";
				case 426:
					return "Measurement fail.\nCheck probe/display setting.";
				case 427:
					return "Port already used.";
				case 428:
					return "Cannot cal channel 0.";
				case 429:
					return "No Output Probe have been specified.";
				case 503:
					return "Unacceotable calibration data.";
				case 504:
					return "Unacceptable analog range data.";
				case 506:
					return "Matrix calibration error.";
				case 509:
					return "Invalid command\n or Remote off.";
				case 515:
					return "Hold error.";
				case 520:
					return "No sync_signal.";
				case 521:
					return "Too bright";
				case 522:
					return "Over\nSet a color within CA's measuring range.";
				case 523:
					return "Offset error.\nPerform zero calibration.";
				case 524:
					return "Over in TduvLv mode.";
				case 553:
					return "Flicker error\nCheck probe type.";
				default:
					reopenRequired = true;
					return "ERROR:" + errorParam;
			}
		}

		/// <summary>Get error No.</summary>
		/// <remarks>
		/// This error No. is defined by CA-SDK.
		/// </remarks>
		/// <returns>reeor No.</returns>
		public Int32 getErrorNo() {
			switch (errorNo) {
				case NotPowerOn:
				case FileCheckError:
				case OtherError:
					return (errorNo);
			}
			return (errorNo & 0xffff);
		}

		/// <summary>Initializing CA-SDK</summary>
		/// <param name="id">CA ID</param>
		/// <param name="numberOfProbes">No. of probes</param>
		/// <param name="initialRetry">No. of Initialize retry</param>
		/// <param name="portVal">Port value</param>
		/// <returns>result</returns>
		public bool open(Int32 id, Int32 numberOfProbes, Int32 initialRetry, Int32 portVal) {
			return open(id, numberOfProbes, initialRetry, portVal, 9600);
		}

		/// <summary>Initializing CA-SDK</summary>
		/// <param name="id">CA ID</param>
		/// <param name="numberOfProbes">No. of probes</param>
		/// <param name="initialRetry">No. of Initialize retry</param>
		/// <param name="portVal">Port value</param>
		/// <param name="baudRate">BAUD RATE(for RS-232C interface)</param>
		/// <returns>result</returns>
		public bool open(Int32 id, Int32 numberOfProbes, Int32 initialRetry, Int32 portVal, Int32 baudRate) {
			bool status = true;
			Int32 retry;
			String dummyStr;
			ca210ID = id;
			try
			{ Ca210 = new Ca200(); }
			catch
            { return false; }
			
			for (retry = 0; retry < initialRetry; retry++) {
				status = true;
				dummyStr = "";
				for (Int32 i = 0; i < numberOfProbes; i++)
					dummyStr += (i + 1).ToString();
				try {
					Ca210.SetConfiguration(ca210ID, dummyStr, portVal, baudRate);
				}
				catch {
					errorNo = NotPowerOn;
					status = false;
				}
				if (status == true)
					break;
				System.Threading.Thread.Sleep(500);
			}
			if (status == true) {
				try {
					cas = Ca210.Cas;
					ca = cas[ca210ID];
					memory = ca.Memory;
					probes = ca.Probes;
					probe = new Probe[numberOfProbes];
					for (Int32 i = 0; i < numberOfProbes; i++)
						probe[i] = probes[i + 1];

				}
				catch (COMException cex) {
					errorNo = cex.ErrorCode;
					errorParam = cex.Message;
					status = false;
				}
				catch (Exception ex) {
					errorNo = OtherError;
					errorParam = ex.Message;
					status = false;
				}
			}
			if (status == true) {
				outputProbes = ca.OutputProbes;
				outputProbes.AddAll();
				nowDisplayMode = (DisplayMode)ca.DisplayMode;
			}
			if (status != true)
				close();
			return (status);
		}

		/// <summary>Terminate CA-SDK</summary>
		/// <returns>result</returns>
		public bool close() {
			try {
				if (probe != null) {
					for (Int32 i = 0; i < probe.GetLength(0); i++) {
						if (probe[i] != null) {
							System.Runtime.InteropServices.Marshal.FinalReleaseComObject(probe[i]);
						}
					}
				}
				if (probes != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(probes);
				if (memory != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(memory);
				if (outputProbes != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(outputProbes);
				if (ca != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(ca);
				if (cas != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(cas);
				if (Ca210 != null)
					System.Runtime.InteropServices.Marshal.FinalReleaseComObject(Ca210);
			}
			catch { }
			finally {
				if (probe != null) {
					for (Int32 i = 0; i < probe.GetLength(0); i++) {
						if (probe[i] != null) {
							probe[i] = null;
						}
					}
				}
				probes = null;
				memory = null;
				outputProbes = null;
				ca = null;
				cas = null;
				Ca210 = null;
			}
			return true;
		}

		#region Cas correction

		/// <summary>Sends the Measure command to all connected CA units.</summary>
		/// <returns>result</returns>
		public bool sendMsr() {
			try {
				cas.SendMsr();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Receives measurement result from all connext CA units.</summary>
		/// <returns>result</returns>
		public bool receiveMsr() {
			try {
				cas.ReceiveMsr();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Sets an ID name(alias) for a specified CA object.</summary>
		/// <param name="idString">ID name for CA object</param>
		/// <returns>result</returns>
		public bool setCalId(String idString) {
			try {
				cas.SetCaID(ca210ID, idString);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		#endregion

		#region Ca object

		/// <summary>Specified the display probe.</summary>
		/// <param name="probeId">Probe ID of the probe being set as display probe</param>
		/// <returns>result</returns>
		public bool setDisplayProbe(String probeId) {
			try {
				ca.DisplayProbe = probeId;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the display probe ID</summary>
		/// <param name="probeId">Probe ID of the display probe</param>
		/// <returns>result</returns>
		public bool getDisplayProve(out String probeId) {
			try {
				probeId = ca.DisplayProbe;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				probeId = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				probeId = "";
				return false;
			}
			return true;
		}

		/// <summary>Get the sync mode</summary>
		/// <param name="syncMode">Sync mode(enum SyncMode or frequency(float data)</param>
		/// <returns>result</returns>
		public bool setSyncMode(float syncMode) {
			try {
				ca.SyncMode = (float)syncMode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Set the sync mode</summary>
		/// <param name="syncMode">Sync mode(enum SyncMode or frequency(float data)</param>
		/// <returns>result</returns>
		public bool getSyncMode(out float syncMode) {
			try {
				syncMode = ca.SyncMode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				syncMode = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				syncMode = 0;
				return false;
			}
			return true;
		}

		/// <summary>Set Display mode</summary>
		/// <param name="mode">Display mode</param>
		/// <returns>result</returns>
		public bool setDisplayMode(DisplayMode mode) {
			try {
				ca.DisplayMode = (int)mode;
				nowDisplayMode = mode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Get Display mode</summary>
		/// <param name="mode">Display mode</param>
		/// <returns>result</returns>
		public bool getDisplayMode(out DisplayMode displayMode) {
			try {
				displayMode = (DisplayMode)ca.DisplayMode;
				nowDisplayMode = displayMode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				displayMode = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				displayMode = 0;
				return false;
			}
			return true;
		}

		/// <summary>Set the number of digits display.</summary>
		/// <param name="digits">Display digits</param>
		/// <returns>result</returns>
		public bool setDisplayDigits(DisplayDigits digits) {
			try {
				ca.DisplayDigits = (Int32)digits;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Get the number of digits display.</summary>
		/// <param name="digits">Display digits</param>
		/// <returns>result</returns>
		public bool getDisplayDigits(out DisplayDigits digits) {
			try {
				digits = (DisplayDigits)ca.DisplayDigits;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				digits = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				digits = 0;
				return false;
			}

			return true;
		}

		/// <summary>Set averaging mode.</summary>
		/// <param name="averagingMode">Averaging mode</param>
		/// <returns>result</returns>
		public bool setAveragingMode(AveragingMode averagingMode) {
			try {
				ca.AveragingMode = (Int32)averagingMode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Get averaging mode.</summary>
		/// <param name="averagingMode">Averaging mode</param>
		/// <returns>result</returns>
		public bool getAveragingMode(out AveragingMode averagingMode) {
			try {
				averagingMode = (AveragingMode)ca.AveragingMode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				averagingMode = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				averagingMode = 0;
				return false;
			}
			return true;
		}

		/// <summary>Set brightness display unit</summary>
		/// <param name="brightnessUnit">Brightness unit</param>
		/// <returns>result</returns>
		public bool setBrightnessUnit(BrightnessUnit brightnessUnit) {
			try {
				ca.BrightnessUnit = (Int32)brightnessUnit;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Get brightness display unit</summary>
		/// <param name="brightnessUnit">Brightness unit</param>
		/// <returns>result</returns>
		public bool getBrightnessUnit(out BrightnessUnit brightnessUnit) {
			try {
				brightnessUnit = (BrightnessUnit)ca.BrightnessUnit;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				brightnessUnit = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				brightnessUnit = 0;
				return false;
			}
			return true;
		}

		/// <summary>Gets the CA unit's product type.</summary>
		/// <param name="caType">Product tyep information for targeted CA unit</param>
		/// <returns>result</returns>
		public bool getCaType(out String caType) {
			try {
				caType = ca.CAType;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				caType = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				caType = "";
				return false;
			}
			return true;
		}

		/// <summary>Gets the CA unit's firmware version information.</summary>
		/// <param name="version">CA unit's firmware version information</param>
		/// <returns>result</returns>
		public bool getCaVersion(out String version) {
			try {
				version = ca.CAVersion;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				version = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				version = "";
				return false;
			}
			return true;
		}

		/// <summary>Gets the CA unit's ID number</summary>
		/// <param name="portId">CA unit's ID number</param>
		/// <returns>result</returns>
		public bool getNumber(out Int32 number) {
			try {
				number = ca.Number;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				number = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				number = 0;
				return false;
			}
			return true;
		}

		/// <summary>Gets the ID of the CA unit's communication port.</summary>
		/// <param name="portId">CA unit's communication port ID</param>
		/// <returns>result</returns>
		public bool getPortId(out String portId) {
			try {
				portId = ca.PortID;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				portId = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				portId = "";
				return false;
			}
			return true;
		}

		/// <summary>Sets the ID name of the CA unit.</summary>
		/// <param name="id">ID name of CA unit</param>
		/// <returns>result</returns>
		public bool setId(String id) {
			try {
				ca.ID = id;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the ID name of the CA unit.</summary>
		/// <param name="id">ID name of CA unit</param>
		/// <returns>result</returns>
		public bool getId(out String id) {
			try {
				id = ca.ID;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				id = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				id = "";
				return false;
			}
			return true;
		}

		/// <summary>Sets the CA unit's remote mode</summary>
		/// <param name="mode">Remote mode setting</param>
		/// <returns>result</returns>
		public bool setRemoteMode(RemoteMode mode) {
			try {
				ca.RemoteMode = (Int32)mode;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Sets CA unit's default calibration mode.</summary>
		/// <param name="calStandard">Default calibration mode.</param>
		/// <returns>result</returns>
		public bool setCalStandard(CalStandard calStandard) {
			try {
				ca.CalStandard = (Int32)calStandard;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets CA unit's default calibration mode.</summary>
		/// <param name="calStandard">Default calibration mode.</param>
		/// <returns>result</returns>
		public bool getCalStandard(out CalStandard calStandard) {
			try {
				int dat = ca.CalStandard;
				calStandard = (CalStandard)dat;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				calStandard = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				calStandard = 0;
				return false;
			}
			return true;
		}

		/// <summary>Execute zero-calibration.</summary>
		/// <returns>result</returns>
		public bool calZero() {
			try {
				ca.CalZero();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Execute measurement.</summary>
		/// <returns>result</returns>
		public bool measure() {
			try {
				ca.Measure(0);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Sets the range of the CA unit's analog display.</summary>
		/// <param name="range1">Range setting 1</param>
		/// <param name="range2">Range setting2</param>
		/// <returns>result</returns>
		public bool setAnalogRange(float range1, float range2) {
			try {
				ca.SetAnalogRange(range1, range2);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the range of the CA unit's analog display.</summary>
		/// <param name="range1">Range setting 1</param>
		/// <param name="range2">Range setting2</param>
		/// <returns>result</returns>
		public bool getAnalogRange(out float range1, out float range2) {
			try {
				ca.GetAnalogRange(out range1,out range2);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				range1 = 0;
				range2 = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				range1 = 0;
				range2 = 0;
				return false;
			}
			return true;
		}

		/// <summary>Sets the analog display range used by the CA for flicker measurement.</summary>
		/// <param name="range">Range setting</param>
		/// <returns>result</returns>
		public bool setFMAAnalogRange(float range) {
			try {
				ca.SetFMAAnalogRange(range);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the analog display range used by the CA for flicker measurement.</summary>
		/// <param name="range">Range setting</param>
		/// <returns>result</returns>
		public bool getFMAAnalogRange(out float range) {
			try {
				ca.GetFMAAnalogRange(out range);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				range = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				range = 0;
				return false;
			}
			return true;
		}

		/// <summary>Sets the CA into PWRON status.</summary>
		/// <returns>result</returns>
		public bool setPwrOnStatus() {
			try {
				ca.SetPWROnStatus();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Designates the probe that will be used as the CA display probe.</summary>
		/// <param name="probeNo">ID number of probe to be used as display probe</param>
		/// <returns>result</returns>
		public bool setDisplayProbe(Int32 probeNo) {
			try {
				ca.SetDisplayProbe(probeNo);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Sets the Ca unit into display-characteristics input mode.</summary>
		/// <returns>result</returns>
		public bool setAnalyzerCalMode() {
			try {
				ca.SetAnalyzerCalMode();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Takes the CA unit out of display-characteristics input mode,and return int to normal mode.</summary>
		/// <returns>return</returns>
		public bool resetAnalyzerCalMode() {
			try {
				ca.ResetAnalyzerCalMode();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Execute input of display-characteristics data.</summary>
		/// <param name="color">Color for which display-characteristics input data is be enabled</param>
		/// <returns>result</returns>
		public bool resetAnalyzerCalMode(ColorNo color) {
			try {
				ca.SetAnalyzerCalData((Int32)color);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Write calibration data into memory.</summary>
		/// <returns>result</returns>
		public bool enter() {
			try {
				ca.Enter();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Sets the CA unit into calibration mode.</summary>
		/// <returns>result</returns>
		public bool setLvxyCalMode() {
			try {
				ca.SetLvxyCalMode();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Take the CA unit out of arbitrary calibratin mode and returns it to normal mode.</summary>
		/// <returns>result</returns>
		public bool resetLvxyCalMode() {
			try {
				ca.ResetLvxyCalMode();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Execute input of arbitrary calibration data.</summary>
		/// <param name="color">Color for which data is to be entered</param>
		/// <param name="x">x calibration value</param>
		/// <param name="y">y calibration value</param>
		/// <param name="Lv">Lv calibration value</param>
		/// <returns>result</returns>
		public bool setLvxyCalData(ColorNo color, float x, float y, float Lv) {
			try {
				ca.SetLvxyCalData((Int32)color, x, y, Lv);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		#endregion

		#region Momory object

		/// <summary>Specifies the CA unit's memory channel by channel number.</summary>
		/// <param name="channel">Memory channeel selection</param>
		/// <returns>result</returns>
		public bool setChannelNo(Int32 channel) {
			try {
				memory.ChannelNO = channel;
			}
			catch (COMException cex) {
				errorParam = cex.Message;
				errorNo = cex.ErrorCode;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Get the current selection channel number.</summary>
		/// <param name="channel">Currnetly selected memory channel</param>
		/// <returns>result</returns>
		public bool getChannelNo(out Int32 channel) {
			try {
				channel = memory.ChannelNO;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				channel = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				channel = 0;
				return false;
			}
			return true;
		}

		/// <summary>Selects th CA unit's memory channel by channel ID name.</summary>
		/// <param name="id">Memory channel selection</param>
		/// <returns>result</returns>
		public bool setChannelId(String id) {
			try {
				memory.ChannelID = id;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				errorParam = id;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the current selection channel ID name.</summary>
		/// <param name="id">Currently selected memory channel</param>
		/// <returns>result</returns>
		public bool getChannelId(out String id) {
			try {
				id = memory.ChannelID;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				id = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				id = "";
				return false;
			}
			return true;
		}

		/// <summary>Gets the reference(white) color setting for the selected memory channel.</summary>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <param name="x">Reference(white) calor's x value</param>
		/// <param name="y">Reference(white) calor's y value</param>
		/// <param name="Lv">Reference(white) calor's Lv value</param>
		/// <returns>result</returns>
		public bool getReferenceColor(Int32 probeNo, out float x, out float y, out float Lv) {
			try {
				memory.GetReferenceColor(probe[probeNo].ID, out x, out y, out Lv);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				x = 0;
				y = 0;
				Lv = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				x = 0;
				y = 0;
				Lv = 0;
				return false;
			}
			return true;
		}

		/// <summary>Sets other ID name for the currentry selected memory channel.</summary>
		/// <param name="id">ID name setting for the currently selected memory channel</param>
		/// <returns>result</returns>
		public bool setChannelID(String id) {
			try {
				memory.SetChannelID(id);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets calibration information form the current;y selected memory channel.</summary>
		/// <param name="probeNo">Targeted probe's ID number</param>
		/// <param name="calProbeSerial">Calibration probe's serial number</param>
		/// <param name="refProbeSerial">Reference-color probe's serial number</param>
		/// <param name="calMode">Calibration mode</param>
		/// <returns>result</returns>
		public bool getMemoryStatus(Int32 probeNo, out Int32 calProbeSerial, out Int32 refProbeSerial, out Int32 calMode) {
			try {
				memory.GetMemoryStatus(probeNo, out calProbeSerial, out refProbeSerial, out calMode);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				calProbeSerial = 0;
				refProbeSerial = 0;
				calMode = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				calProbeSerial = 0;
				refProbeSerial = 0;
				calMode = 0;
				return false;
			}
			return true;
		}

		/// <summary>Comapres the calibration data in the currently selected memory channel 
		/// against calibration data hels in the specified calibration data file.</summary>
		/// <param name="probeNo">Probe ID number of probe to be checked</param>
		/// <param name="fileName">Numa of calibration data file to be used for comparison.(ful-path)</param>
		/// <returns>result</returns>
		public bool checkCalData(Int32 probeNo, String fileName) {
			bool status = true;
			int st;
			try {
				st = memory.CheckCalData(probeNo, fileName);
				if (st != 0) {
					status = false;
					errorNo = FileCheckError;
				}
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				status = false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}

			return status;
		}

		/// <summary>Copies the calibration data from the currently selected memory channel into a file.</summary>
		/// <param name="probeNo">Probe ID number of probe whose data is to be copied</param>
		/// <param name="fileName">Fliename for destinetion calibration file.(ful-path)</param>
		/// <returns>result</returns>
		public bool copyToFile(Int32 probeNo, String fileName) {
			try {
				memory.CopyToFile(probeNo, fileName);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>copies the calibration data from the specifies file into the currnetly selected memory channel.</summary>
		/// <param name="probeNo">Probe ID number of destination probe</param>
		/// <param name="fileName">Filename of source calibration file(ful-path)</param>
		/// <returns>result</returns>
		public bool copyFromFile(Int32 probeNo, String fileName) {
			try {
				memory.CopyFromFile(probeNo, fileName);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		#endregion

		#region Probes correction

		/// <summary>Gets the coount of the connected probes.</summary>
		/// <param name="numberOfProbes">Number of connected probes</param>
		/// <returns>result</returns>
		public bool getNumberOfProbes(out Int32 numberOfProbes) {
			try {
				numberOfProbes = probe.GetLength(0);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				numberOfProbes = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				numberOfProbes = 0;
				return false;
			}
			return true;
		}

		#endregion

		#region OutputProbes correction

		/// <summary>Get the count of the output probes.</summary>
		/// <param name="numberOfProbes">Number of output probes</param>
		/// <returns>result</returns>
		public bool getNumberOfOutputProbes(out Int32 numberOfProbes) {
			try {
				numberOfProbes = outputProbes.Count;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				numberOfProbes = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				numberOfProbes = 0;
				return false;
			}
			return true;
		}

		/// <summary>Adds a probe to the specified collection of output probes.</summary>
		/// <param name="probeID">ID name of the probe to be added to the collection</param>
		/// <returns>result</returns>
		public bool outputProbesAdd(String probeID) {
			try {
				outputProbes.Add(probeID);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Add all connected probes to the collection of output probes.</summary>
		/// <returns>result</returns>
		public bool outputProbesAddAll() {
			try {
				outputProbes.AddAll();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Deletes all designature of output probes.</summary>
		/// <returns>result</returns>
		public bool outputProbesRemoveAll() {
			try {
				outputProbes.RemoveAll();
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		#endregion

		#region Probe Object

		/// <summary>Get the measurement results.</summary>
		/// <param name="data1">Value of measurement result 1</param>
		/// <param name="data2">Value of measurement result 2</param>
		/// <param name="data3">Value of measurement result 3</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool getData(out double data1, out double data2, out double data3, Int32 probeNo) {
			bool status = true;
			Int32 st = 0;
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error.";
				data1 = 0;
				data2 = 0;
				data3 = 0;
				return false;
			}
			try {
				switch (nowDisplayMode) {
					case DisplayMode.DispModeLvxy:
						data1 = probe[probeNo].sx;
						data2 = probe[probeNo].sy;
						data3 = probe[probeNo].Lv;
						st = probe[probeNo].RD;
						break;
					case DisplayMode.DispModeTdudv:
						data1 = probe[probeNo].T;
						data2 = probe[probeNo].duv;
						data3 = probe[probeNo].Lv;
						st = probe[probeNo].RD;
						break;
					case DisplayMode.DispModeANL:
					case DisplayMode.DispModeANLG:
					case DisplayMode.DispModeANLR:
						data1 = probe[probeNo].R;
						data2 = probe[probeNo].B;
						data3 = probe[probeNo].G;
						st = probe[probeNo].RAD;
						break;
					case DisplayMode.DispModePUV:
						data1 = probe[probeNo].ud;
						data2 = probe[probeNo].vd;
						data3 = probe[probeNo].Lv;
						st = probe[probeNo].RD;
						break;
					case DisplayMode.DispModeFMA:
						data1 = 0;
						data2 = 0;
						data3 = probe[probeNo].FlckrFMA;
						st = probe[probeNo].RFMA;
						break;
					case DisplayMode.DispModeXYZ:
						data1 = probe[probeNo].X;
						data2 = probe[probeNo].Y;
						data3 = probe[probeNo].Z;
						st = probe[probeNo].RD;
						break;
					case DisplayMode.DispModeJEITA:
						data1 = 0;
						data2 = 0;
						data3 = probe[probeNo].FlckrJEITA;
						st = probe[probeNo].RJEITA;
						break;
					default:
						data1 = -1;
						data2 = -1;
						data3 = -1;
						st = OtherError;
						errorParam = "Display mode is unusual.";
						break;
				}
			}
			catch (COMException cex) {
				data1 = 0;
				data2 = 0;
				data3 = 0;
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				status = false;
			}
			catch (Exception ex) {
				data1 = 0;
				data2 = 0;
				data3 = 0;
				errorNo = OtherError;
				errorParam = ex.Message;
				status = false;
			}
			if (st != 0) {
				errorNo = st;
				status = false;
			}
			return (status);
		}

		/// <summary>Gets the probe's ID number.</summary>
		/// <param name="number">Targeted probe's ID number</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool getProbeNumber(out Int32 number, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				number = 0;
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				return false;
			}
			try {
				number = probe[probeNo].Number;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				number = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				number = 0;
				return false;
			}
			return true;
		}

		/// <summary>Gets the probe's Serial No.</summary>
		/// <param name="serial">Targeted probe's Serial number</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns></returns>
		public bool getProbeSerial(out String serial, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				serial = "";
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				return false;
			}
			try {
				serial = probe[probeNo].SerialNO;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				serial = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				serial = "";
				return false;
			}
			return true;
		}

		/// <summary>Sets the ID name for the targeted probe.</summary>
		/// <param name="id">ID name for the targeted probe</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool setProbeId(String id, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				return false;
			}
			try {
				probe[probeNo].ID = id;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				return false;
			}
			return true;
		}

		/// <summary>Gets the ID name for the targeted probe.</summary>
		/// <param name="id">Targeted probe's ID name</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool getProbeId(out String id, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				id = "";
				return false;
			}
			try {
				id = probe[probeNo].ID;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				id = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				id = "";
				return false;
			}
			return true;
		}

		/// <summary>Gets the amplitude of each frequency in the JEITA flicker measuring data.</summary>
		/// <param name="frequency">Frequency</param>
		/// <param name="signal">Amplitude of each frequency</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool GetSpectrum(Int32 frequency, out float signal, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				signal = 0;
				return false;
			}
			try {
				signal = probe[probeNo].GetSpectrum(frequency);
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				signal = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				signal = 0;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Gets the Lv for the targeted probe.
		/// </summary>
		/// <param name="lv">Value</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns></returns>
		public bool getProbeLv(out Double lv, Int32 probeNo)
        {
			if (probe.GetLength(0) <= probeNo)
			{
				lv = double.NaN;
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				return false;
			}
			try
			{
				lv = probe[probeNo].Lv;
			}
			catch (COMException cex)
			{
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				lv = double.NaN;
				return false;
			}
			catch (Exception ex)
			{
				errorNo = OtherError;
				errorParam = ex.Message;
				lv = double.NaN;
				return false;
			}
			return true;
		}

		#endregion

		#region IProbeInfo object

		/// <summary>Obtains a character string that indicates the type of the connexted probes,</summary>
		/// <param name="typeName">A character string of the type to be obtained</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool getTypeName(out String typeName, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				typeName = "";
				return false;
			}
			try {
				IProbeInfo info = (IProbeInfo)probe[probeNo];
				typeName = info.TypeName;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				typeName = "";
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				typeName = "";
				return false;
			}
			return true;
		}

		/// <summary>Obtaions a value that indicates the type of the connected probe.</summary>
		/// <param name="typeNo">Value of  the type tobe obtained</param>
		/// <param name="probeNo">Number of targeted probe</param>
		/// <returns>result</returns>
		public bool getTypeNo(out Int32 typeNo, Int32 probeNo) {
			if (probe.GetLength(0) <= probeNo) {
				errorNo = OtherError;
				errorParam = "PROBE No. error";
				typeNo = 0;
				return false;
			}
			try {
				IProbeInfo info = (IProbeInfo)probe[probeNo];
				typeNo = info.TypeNO; ;
			}
			catch (COMException cex) {
				errorNo = cex.ErrorCode;
				errorParam = cex.Message;
				typeNo = 0;
				return false;
			}
			catch (Exception ex) {
				errorNo = OtherError;
				errorParam = ex.Message;
				typeNo = 0;
				return false;
			}
			return true;
		}

		#endregion

	}
}
