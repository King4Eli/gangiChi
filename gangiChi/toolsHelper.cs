using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic; 
using System.IO; 
using System.Runtime.InteropServices;
using System.Security.Cryptography; 
using System.Threading; 

namespace toolsHelper
{
    public class H
    {
        private static readonly Random _newRandom = new Random();
        //console.writeline
        public static void Print<T>(T v = default,  ConsoleColor c = ConsoleColor.White)
        {
            Console.ForegroundColor = c;
            Console.Write(v);
            Console.ResetColor();
        }
        public static void PrintL<T>(T v = default, ConsoleColor c = ConsoleColor.White)
        {
            Print(v,c);
            Console.Write("\n");
        }

        //get params
        public static Dictionary<string, string> ParseArguments(string[] args_)
        {
            var argDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < args_.Length; i++)
            {
                string arg = args_[i];

                if (arg.StartsWith("-"))
                {
                    string value = (i + 1 < args_.Length && !args_[i + 1].StartsWith("-")) ? args_[i + 1] : null;
                    argDictionary[arg] = value;
                }
            }

            return argDictionary;
        }

        //make hash
        public static class Hash
        {
            public static string SHA384_(byte[] byeta)
            {
                using (SHA384 sha384Hash = SHA384.Create())
                {
                    byte[] sha384HashBytes = sha384Hash.ComputeHash(byeta);
                    string sha384HashResult = BitConverter.ToString(sha384HashBytes).Replace("-", String.Empty);
                    return sha384HashResult;
                }
            }
            public static string SHA256_(byte[] byet)
            {
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    byte[] sha256HashBytes = sha256Hash.ComputeHash(byet);
                    string sha256HashResult = BitConverter.ToString(sha256HashBytes).Replace("-", String.Empty);
                    return sha256HashResult;
                }
            }
            public static string SHA512_(byte[] byet)
            {
                using (SHA512 sha512Hash = SHA512.Create())
                {
                    byte[] sha512HashBytes = sha512Hash.ComputeHash(byet);
                    string sha512HashResult = BitConverter.ToString(sha512HashBytes).Replace("-", String.Empty);
                    return sha512HashResult;
                }
            }

            public static string MD5_(byte[] byet)
            {
                using (MD5 MD5Hash = MD5.Create())
                {
                    byte[] MD5HashBytes = MD5Hash.ComputeHash(byet);
                    string MD5HashResult = BitConverter.ToString(MD5HashBytes).Replace("-", String.Empty);
                    return MD5HashResult;
                }
            }
            public static string SHA1_(byte[] byet)
            {
                // Compute SHA-256 hash 
                using (SHA1 SHA1Hash = SHA1.Create())
                {
                    byte[] SHA1HashBytes = SHA1Hash.ComputeHash(byet);
                    string SHA1HashResult = BitConverter.ToString(SHA1HashBytes).Replace("-", String.Empty);
                    return SHA1HashResult;
                }
            }
        }

        //shred file
        public class Shred_File
        {
            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern bool FlushFileBuffers(SafeFileHandle handle);
            public static bool Shred(string filePath, int overwriteIliteration = 4)
            {
                if (File.Exists(filePath))
                {
                    //long fileSize = new FileInfo(filePath).Length;
                    byte[] randomBytes = new byte[_newRandom.Next(50, 100)];

                    try
                    {
                        // Fill with random data
                        _newRandom.NextBytes(randomBytes);
                        File.WriteAllBytes(filePath, randomBytes);
                        Thread.Sleep(10);
                        // Write random data to file multiple times
                        for (int i = 0; i < overwriteIliteration; i++)
                        {
                            _newRandom.NextBytes(randomBytes);
                            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                            {
                                fileStream.Write(randomBytes, 0, randomBytes.Length);
                                fileStream.Flush(); // Flush FileStream buffers

                                // Flush OS buffers to disk
                                if (!FlushFileBuffers(fileStream.SafeFileHandle))
                                {
                                    throw new IOException("Failed to flush file buffers.");
                                }
                            }
                            using (var fileStream = new FileStream(filePath, FileMode.Truncate, FileAccess.Write))
                            {
                                fileStream.SetLength(0);
                            }
                            Thread.Sleep(10);
                            // Modify file timestamps
                            File.SetCreationTime(filePath, new DateTime(1984, 2, 5));
                            File.SetLastWriteTime(filePath, new DateTime(1984, 2, 5));
                            File.SetLastAccessTime(filePath, new DateTime(1984, 2, 5));

                            Thread.Sleep(10);
                        }
                        File.Delete(filePath);
                        return true;
                    }
                    catch (IOException ioEx)
                    {
                        throw new IOException($"File I/O error: {ioEx.Message}");
                    }
                    catch (UnauthorizedAccessException unAuthEx)
                    {
                        throw new IOException($"Permission error: {unAuthEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        throw new IOException($"Error: {ex.Message}");
                    }
                }
                else
                {
                    throw new IOException("File does not exist.");
                } 
            }
        }
        //get random string
        public static string RandomString(int length, bool includeUppercase = false)
        {
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz0123456789";
            const string uppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string chars = includeUppercase ? uppercaseChars + lowercaseChars : lowercaseChars;
            char[] stringChars = new char[length];
            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[_newRandom.Next(chars.Length)];
            }
            return new string(stringChars);
        }

        //desktop key hook
        public class GlobalKeyboardHook
        {
            private Action<IntPtr, int> _keyPressAction;
            private const int WH_KEYBOARD_LL = 13;
            private const int WM_KEYUP = 0x0101;

            private delegate int LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            private IntPtr _hookID = IntPtr.Zero;
            private LowLevelKeyboardProc _proc;

            public GlobalKeyboardHook()
            {
                _proc = HookCallback;
            }

            /// <summary>
            /// wParam is keyup or keydown, vkCode is key pressed
            /// </summary>
            /// <param name="keyPressAction"></param>
            public void Hook(Action<IntPtr, int> keyPressAction)
            {
                _keyPressAction = keyPressAction;
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
            }

            public void Unhook()
            {
                UnhookWindowsHookEx(_hookID);
            }

            private int HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);


                    _keyPressAction?.Invoke(wParam, vkCode);
                }

                return (int)CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
        }

         
    }
}