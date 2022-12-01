using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SNSS_Reader
{
	/// <summary> Snss is a binary Database Format used e.g. by the open-source Chromium Browser to store its session Info </summary>
	/// <remarks>
	/// Session-Info is stored into the Session Folder in the User's AppData-Directory:
	/// the “Current Tabs”, “Current Session”, “Last Tabs” and “Last Session” files.
	/// 
	/// This Class reads the whole File into the <see cref="Commands"/> List.
	/// <a href='https://digitalinvestigation.wordpress.com/2012/09/03/chrome-session-and-tabs-files-and-the-puzzle-of-the-pickle/'
	/// >describes </a>
	/// </remarks>
	public class Snss
	{
		/// <summary>
		/// AKA SessionCommand; Stores Back-Forward History for a Tab.
		/// </summary>
		public struct Command
		{
			public byte Id;

			/// <summary> Browser-Command with <see cref="Content"/> </summary>
			/// <remarks>
			/// either <see cref="Tab"/> or other <see cref="Snss"/> record
			/// </remarks>
			public object Content;

			public Command(byte[] data)
			{
				Id = data[0];

				var content = new byte[data.Length - 1];
				Array.Copy(data, 1, content, 0, content.Length);

				Content = Id == 1 || Id == 6 ? (object) new Tab(content) : content;
			}

			public override string ToString() => Content is Tab tab ? tab.ToString() : Snss.ToString((byte[]) Content);
		}

		[Flags]
		public enum TransitionType : uint
		{
			/// <summary>	User arrived at this page by clicking a link on another page	</summary>
			Link = 0,

			/// <summary>	User typed URL into the Omnibar, or clicked a suggested URL in the Omnibar	</summary>
			Typed = 1,

			/// <summary>	User arrived at page through a  bookmark or similar (eg. “most visited” suggestions on a new tab)	</summary>
			BookMark = 2,

			/// <summary>	Automatic navigation within a sub frame (eg an embedded ad)	</summary>
			SubFrame = 3,

			/// <summary>	Manual navigation in a sub frame	</summary>
			Manual = 4,

			/// <summary>	User selected suggestion from Omnibar (ie. typed part of an address or search term then selected a suggestion which was not a URL)	</summary>
			Suggestion = 5,

			/// <summary>	Start page (or specified as a command line argument)	</summary>
			StartPage = 6,

			/// <summary>	User arrived at this page as a result of submitting a form	</summary>
			Submit = 7,

			/// <summary>	Page was reloaded; either by clicking the refresh button, hitting F5 or hitting enter in the address bar. Also given this transition type if the tab was opened as a result of restoring a previous session.	</summary>
			ReLoad = 8,

			/// <summary>	Generated as a result of a keyword search, not using the default search provider (for example using tab-to-search on Wikipedia). Additionally a transition of type 10 (see below) may also be generated for the url: http:// + keyword	</summary>
			KeyWordSearch = 9,

			/// <summary>	See above	</summary>
			KeyWord = 10,

			/// <summary>	User used the back or forward buttons to arrive at this page	</summary>
			BackFwd = 0x01000000,

			/// <summary>	User used the address bar to trigger this navigation	</summary>
			AddressBar = 0x02000000,

			/// <summary>	User is navigating to the homepage	</summary>
			HomePage = 0x04000000,

			/// <summary>	The beginning of a navigation chain	</summary>
			StartNav = 0x10000000,

			/// <summary>	Last transition in a redirect chain	</summary>
			LastRedirect = 0x20000000,

			/// <summary>	Transition was a client-side redirect (eg. caused by JavaScript or a meta-tag redirect)	</summary>
			ClientRedirect = 0x40000000,

			/// <summary>	Transition was a server-side redirect (ie a redirect specified in the HTTP response header)	</summary>
			ServerRedirect = 0x80000000,

		}

		/// <summary> Browser Tab Data with <see cref="URL"/> and <see cref="TabState"/> </summary>
		public struct Tab
		{
			public int Id;

			/// <summary> current Index in this tab’s back-forward list </summary>
			public int Index;

			/// <summary> 32 bit string length followed by ASCII Bytes </summary>
			public string URL;

			/// <summary> 32 bit string length followed by a UTF-16 string </summary>
			public string Title; //UTF-16

			public TabState State;

			/// <summary>
			/// <a href='http://src.chromium.org/viewvc/chrome/trunk/src/content/public/common/page_transition_types.h?view=markup'></a>
			/// </summary>
			public int TransitionType;

			/// <summary> Flag 1 if the page has POST data, otherwise 0 to prevent accidental re-POST </summary>
			public int POST;

			public string ReferrerURL; //ASCII
			public int ReferencePolicy;
			public string OriginalRequestURL; //ASCII

			/// <summary> 1 if the user-agent was overridden, otherwise 0 </summary>
			public int UserAgent;

			public Tab(byte[] data)
			{
				var urlLength = BitConverter.ToInt32(data, 12);
				var titleOffset = urlLength % 4 == 0 ? urlLength + 16 : urlLength / 4 * 4 + 20;
				var titleLength = BitConverter.ToInt32(data, titleOffset) * 2;
				var stateOffset = titleLength % 4 == 0
					? titleLength + titleOffset + 4
					: titleLength / 4 * 4 + titleOffset + 8;
				var stateLength = BitConverter.ToInt32(data, stateOffset);
				var transitionTypeOffset = stateLength % 4 == 0
					? stateLength + stateOffset + 4
					: stateLength / 4 * 4 + stateOffset + 8;
				var refURLLength = BitConverter.ToInt32(data, transitionTypeOffset + 8);
				var refPolicyOffset = refURLLength % 4 == 0
					? refURLLength + transitionTypeOffset + 12
					: refURLLength / 4 * 4 + transitionTypeOffset + 16;
				var reqURLLength = BitConverter.ToInt32(data, refPolicyOffset + 4);
				var userAgentOffset = reqURLLength % 4 == 0
					? reqURLLength + refPolicyOffset + 8
					: reqURLLength / 4 * 4 + refPolicyOffset + 12;

				Id = BitConverter.ToInt32(data, 4);
				Index = BitConverter.ToInt32(data, 8);
				URL = Encoding.ASCII.GetString(data, 16, urlLength);
				Title = Encoding.Unicode.GetString(data, titleOffset + 4, titleLength);
				var state = new byte[stateLength];
				Array.Copy(data, stateOffset + 4, state, 0, state.Length);
				if (stateLength <= 0)
				{
					State = default;
					TransitionType = 0;
					POST = 0;
					ReferrerURL = null;
					ReferencePolicy = 0;
					OriginalRequestURL = null;
					UserAgent = 0;
					return;
				}
				State = new TabState(state);
				TransitionType = BitConverter.ToInt32(data, transitionTypeOffset);
				POST = BitConverter.ToInt32(data, transitionTypeOffset + 4);
				ReferrerURL = Encoding.ASCII.GetString(data, transitionTypeOffset + 12, refURLLength);
				ReferencePolicy = BitConverter.ToInt32(data, refPolicyOffset);
				OriginalRequestURL = Encoding.ASCII.GetString(data, refPolicyOffset + 8, reqURLLength);
				UserAgent = BitConverter.ToInt32(data, userAgentOffset);
			}

			public override string ToString()
			{
				var strBuilder = new StringBuilder();

				strBuilder.AppendLine("Id: " + Id);
				strBuilder.AppendLine("Index: " + Index);
				strBuilder.AppendLine("URL: " + URL);
				strBuilder.AppendLine("Title: " + Title);

				strBuilder.AppendLine("Transition type: 0x" + TransitionType.ToString("X8"));
				switch (TransitionType & 0xFF)
				{
					case 0:
						strBuilder.AppendLine("  User arrived at this page by clicking a link on another page.");
						break;
					case 1:
						strBuilder.AppendLine(
							"  User typed URL into the Omnibar, or clicked a suggested URL in the Omnibar.");
						break;
					case 2:
						strBuilder.AppendLine(
							"  User arrived at page through a  bookmark or similar (eg. \"most visited\" suggestions on a new tab).");
						break;
					case 3:
						strBuilder.AppendLine("  Automatic navigation within a sub frame (eg an embedded ad).");
						break;
					case 4:
						strBuilder.AppendLine("  Manual navigation in a sub frame.");
						break;
					case 5:
						strBuilder.AppendLine(
							"  User selected suggestion from Omnibar (ie. typed part of an address or search term then selected a suggestion which was not a URL).");
						break;
					case 6:
						strBuilder.AppendLine("  Start page (or specified as a command line argument).");
						break;
					case 7:
						strBuilder.AppendLine("  User arrived at this page as a result of submitting a form.");
						break;
					case 8:
						strBuilder.AppendLine(
							"  Page was reloaded; either by clicking the refresh button, hitting F5, hitting enter in the address bar or as result of restoring a previous session.");
						break;
					case 9:
						strBuilder.AppendLine(
							"  Generated as a result of a keyword search, not using the default search provider (for example using tab-to-search on Wikipedia).");
						break;
					/*case 10:
					    strBuilder.AppendLine("  10.");
					    break;*/
					default:
						strBuilder.AppendLine("  " + (TransitionType & 0xFF) + ".");
						break;
				}
				if ((TransitionType & 0x01000000) == 0x01000000)
					strBuilder.AppendLine("  User used the back or forward buttons to arrive at this page.");
				if ((TransitionType & 0x02000000) == 0x02000000)
					strBuilder.AppendLine("	 User used the address bar to trigger this navigation.");
				if ((TransitionType & 0x04000000) == 0x04000000)
					strBuilder.AppendLine("  User is navigating to the homepage.");
				if ((TransitionType & 0x10000000) == 0x10000000)
					strBuilder.AppendLine("  The beginning of a navigation chain.");
				if ((TransitionType & 0x20000000) == 0x20000000)
					strBuilder.AppendLine("  Last transition in a redirect chain.");
				if ((TransitionType & 0x40000000) == 0x40000000)
					strBuilder.AppendLine(
						"  Transition was a client-side redirect (eg. caused by JavaScript or a meta-tag redirect).");
				if ((TransitionType & 0x80000000) == 0x08000000)
					strBuilder.AppendLine(
						"  Transition was a server-side redirect (ie a redirect specified in the HTTP response header).");

				if (POST == 0)
					strBuilder.AppendLine("The page has no POST data.");
				else if (POST == 1)
					strBuilder.AppendLine("The page has POST data.");
				else
					strBuilder.AppendLine("POST: " + POST);

				strBuilder.AppendLine("Referrer URL: " + ReferrerURL);
				strBuilder.AppendLine("Referrer’s Policy: " + ReferencePolicy);
				strBuilder.AppendLine("Original Request URL: " + OriginalRequestURL);

				if (UserAgent == 0)
					strBuilder.AppendLine("The user-agent was not overridden.");
				else if (UserAgent == 1)
					strBuilder.AppendLine("The user-agent was overridden.");
				else
					strBuilder.AppendLine("User-agent: " + UserAgent);

				strBuilder.AppendLine("States:");
				strBuilder.Append(State.ToString());

				return strBuilder.ToString();
			}
		}

		public struct TabState
		{
			public struct StateV27 //Version 27 and 28
			{
				public string ValueA; //UTF-16
				public string ValueB; //UTF-16
				public string ValueC; //UTF-16
				public byte[] ValueD;
				public List<string> ValueE;
				public long ValueF;
				public byte[] ValueG;
				public long ValueH;
				public long ValueI;
				public byte[] ValueJ;
				public byte[] ValueK;
				public string ValueL; //ASCII, only in version 28
			}

			public int Version;
			public object States;

			public TabState(byte[] data)
			{
				Version = BitConverter.ToInt32(data, 4);
				var m0 = BitConverter.ToInt64(data, 12);
				var m1 = BitConverter.ToInt64(data, 20);
				var m2 = BitConverter.ToInt64(data, 28);
				var m3 = BitConverter.ToInt64(data, 36);

				if ((Version == 27 || Version == 28) &&
				    m0 == 0x18 &&
				    m1 == 0x10 &&
				    m2 == 0x10 &&
				    m3 == 0x08)
				{
					var states = new List<StateV27>();
					var offset = 44;
					while (offset < data.Length)
					{
						var state = new StateV27();
						var offsetA = BitConverter.ToInt32(data, offset + 8);
						var offsetB = BitConverter.ToInt32(data, offset + 16);
						var offsetC = BitConverter.ToInt32(data, offset + 24);
						var offsetD = BitConverter.ToInt32(data, offset + 32);
						var offsetE = BitConverter.ToInt32(data, offset + 40) + 40;
						state.ValueF = BitConverter.ToInt64(data, offset + 48);
						var offsetG = BitConverter.ToInt32(data, offset + 56);
						state.ValueH = BitConverter.ToInt64(data, offset + 64);
						state.ValueI = BitConverter.ToInt64(data, offset + 72);
						var offsetJ = BitConverter.ToInt32(data, offset + 80) + 80;
						var offsetK = BitConverter.ToInt32(data, offset + 88) + 88;
						var offsetL = Version == 28 ? BitConverter.ToInt32(data, offset + 96) : 0;

						var lengthE = BitConverter.ToInt32(data, offset + offsetE + 4);
						var lengthJ = BitConverter.ToInt32(data, offset + offsetJ);
						var lengthK = BitConverter.ToInt32(data, offset + offsetK);

						if (offsetA != 0)
						{
							offsetA += 8;
							var lengthA = BitConverter.ToInt32(data, offset + offsetA + 20) * 2;
							state.ValueA = Encoding.Unicode.GetString(data, offset + offsetA + 24, lengthA);
						}
						else
							state.ValueA = "";

						if (offsetB != 0)
						{
							offsetB += 16;
							var lengthB = BitConverter.ToInt32(data, offset + offsetB + 20) * 2;
							state.ValueB = Encoding.Unicode.GetString(data, offset + offsetB + 24, lengthB);
						}
						else
							state.ValueB = "";

						if (offsetC != 0)
						{
							offsetC += 24;
							var lengthC = BitConverter.ToInt32(data, offset + offsetC + 20) * 2;
							state.ValueC = Encoding.Unicode.GetString(data, offset + offsetC + 24, lengthC);
						}
						else
							state.ValueC = "";

						if (offsetD != 0)
						{
							offsetD += 32;
							var lengthD = BitConverter.ToInt32(data, offset + offsetD);
							state.ValueD = new byte[lengthD];
							Array.Copy(data, offset + offsetD, state.ValueD, 0, state.ValueD.Length);
						}
						else
							state.ValueD = new byte[0];

						state.ValueE = new List<string>(lengthE);
						for (var i = 0; i < lengthE; i++)
						{
							var offsetStr = BitConverter.ToInt32(data, offset + offsetE + (8 * (i + 1))) +
							                (8 * (i + 1));
							var lengthStr = BitConverter.ToInt32(data, offset + offsetE + offsetStr + 20) * 2;
							state.ValueE.Add(Encoding.Unicode.GetString(data, offset + offsetE + offsetStr + 24,
								lengthStr));
						}

						if (offsetG != 0)
						{
							offsetG += 56;
							var lengthG = offsetJ - offsetG;
							state.ValueG = new byte[lengthG];
							Array.Copy(data, offset + offsetG, state.ValueG, 0, state.ValueG.Length);
						}
						else
							state.ValueG = new byte[0];

						state.ValueJ = new byte[lengthJ];
						Array.Copy(data, offset + offsetJ, state.ValueJ, 0, state.ValueJ.Length);

						state.ValueK = new byte[lengthK];
						Array.Copy(data, offset + offsetK, state.ValueK, 0, state.ValueK.Length);

						if (offsetL != 0)
						{
							offsetL += 96;
							var lengthL = BitConverter.ToInt32(data, offset + offsetL + 4);
							state.ValueL = Encoding.ASCII.GetString(data, offset + offsetL + 8, lengthL);
							lengthL += 8;
							offset += offsetL + (lengthL % 8 == 0 ? lengthL : lengthL / 8 * 8 + 8);
						}
						else
						{
							state.ValueL = "";
							offset += offsetK + lengthK;
						}

						states.Add(state);
					}
					States = states;
				}
				else
				{
					var states = new byte[data.Length - 8];
					Array.Copy(data, 8, states, 0, states.Length);
					States = states;
				}
			}

			public override string ToString()
			{
				var strBuilder = new StringBuilder();

				strBuilder.AppendLine("  Version: " + Version);

				if (States is List<StateV27> states)
				{
					for (var i = 0; i < states.Count; i++)
					{
						strBuilder.AppendLine("  State " + i + ":");
						strBuilder.AppendLine("    Value A: " + states[i].ValueA);
						strBuilder.AppendLine("    Value B: " + states[i].ValueB);
						strBuilder.AppendLine("    Value C: " + states[i].ValueC);
						strBuilder.Append("    Value D: ");
						strBuilder.AppendLine(Snss.ToString(states[i].ValueD));
						strBuilder.AppendLine("    Value E:");
						for (var j = 0; j < states[i].ValueE.Count; j++)
							strBuilder.AppendLine("      Index " + j + ": " + states[i].ValueE[j]);
						strBuilder.AppendLine("    Value F: 0x" + states[i].ValueF.ToString("X16"));
						strBuilder.Append("    Value G: ");
						strBuilder.AppendLine(Snss.ToString(states[i].ValueG));
						strBuilder.AppendLine("    Value H: 0x" + states[i].ValueH.ToString("X16"));
						strBuilder.AppendLine("    Value I: 0x" + states[i].ValueI.ToString("X16"));
						strBuilder.Append("    Value J: ");
						strBuilder.AppendLine(Snss.ToString(states[i].ValueJ));
						strBuilder.Append("    Value K: ");
						strBuilder.AppendLine(Snss.ToString(states[i].ValueK));
						strBuilder.AppendLine("    Value L: " + states[i].ValueL);
					}
				}
				else
				{
					strBuilder.Append("  Value: ");
					strBuilder.Append(Snss.ToString((byte[]) States));
				}

				return strBuilder.ToString();
			}
		}

		public string FileName;
		public int Version;
		public readonly List<Command> Commands = new List<Command>();

		public Snss(string filename) : this(File.Open(filename, FileMode.Open)) => FileName = filename;

		public Snss(Stream fs)
		{
			var magic = new byte[4];
			fs.Read(magic, 0, 4);

			if (magic[0] == 0x53 &&
			    magic[1] == 0x4E &&
			    magic[2] == 0x53 &&
			    magic[3] == 0x53)
			{
				var version = new byte[4];
				fs.Read(version, 0, 4);
				Version = BitConverter.ToInt32(version, 0);
				while (fs.Position < fs.Length)
				{
					var command = ReadCommandBytes(fs);
					Commands.Add(new Command(command));
				}
			}
			fs.Close();
		}

		static byte[] ReadCommandBytes(Stream fs)
		{
			var cmdSizeBytes = new byte[2];
			fs.Read(cmdSizeBytes, 0, 2);
			var commandSize = BitConverter.ToUInt16(cmdSizeBytes, 0);
			var command = new byte[commandSize];
			fs.Read(command, 0, commandSize);
			return command;
		}

		public override string ToString()
		{
			var strBuilder = new StringBuilder();

			strBuilder.AppendLine("File: " + FileName);
			if (Version != 0)
			{
				strBuilder.AppendLine("Version: " + Version);
				strBuilder.AppendLine("Session commands: " + Commands.Count);
			}
			else
				strBuilder.AppendLine("It is not an file with Snss format.");

			return strBuilder.ToString();
		}

		static string ToString(ICollection<byte> value)
		{
			var hex = new StringBuilder(value.Count * 2);
			foreach (var b in value)
			{
				hex.AppendFormat("{0:X2} ", b);
			}
			return hex.ToString();
		}
	}
}