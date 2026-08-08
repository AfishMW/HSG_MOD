
import sys
import os
import json
import urllib.request
import ssl
import ctypes
from ctypes import wintypes

REPO_LIGHT_OWNER = "AfishMW"
REPO_LIGHT_NAME   = "HSG_MOD"

REPO_LID_OWNER   = "hvtXsvc"
REPO_LID_NAME    = "LightInDark_API"

def is_process_running(name):
    TH32CS_SNAPPROCESS = 0x00000002
    class PROCESSENTRY32W(ctypes.Structure):
        _fields_ = [
            ("dwSize", wintypes.DWORD),
            ("cntUsage", wintypes.DWORD),
            ("th32ProcessID", wintypes.DWORD),
            ("th32DefaultHeapID", ctypes.POINTER(ctypes.c_ulong)),
            ("th32ModuleID", wintypes.DWORD),
            ("cntThreads", wintypes.DWORD),
            ("th32ParentProcessID", wintypes.DWORD),
            ("pcPriClassBase", ctypes.LONG),
            ("dwFlags", wintypes.DWORD),
            ("szExeFile", wintypes.WCHAR * 260)
        ]
    kernel32 = ctypes.windll.kernel32
    snap = kernel32.CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0)
    if snap == -1:
        return False
    entry = PROCESSENTRY32W()
    entry.dwSize = ctypes.sizeof(PROCESSENTRY32W)
    found = False
    if kernel32.Process32FirstW(snap, ctypes.byref(entry)):
        while True:
            if entry.szExeFile.lower() == name.lower():
                found = True
                break
            if not kernel32.Process32NextW(snap, ctypes.byref(entry)):
                break
    kernel32.CloseHandle(snap)
    return found

def find_bepinex(start_dir):
    for _ in range(4):
        bep = os.path.join(start_dir, "BepInEx")
        if os.path.isdir(bep):
            return bep
        parent = os.path.dirname(start_dir)
        if parent == start_dir:
            break
        start_dir = parent
    return None

def get_local_versions(plugins_dir):
    ver_file = os.path.join(plugins_dir, "version.json")
    if not os.path.isfile(ver_file):
        return None
    try:
        with open(ver_file, "r", encoding="utf-8") as f:
            data = json.load(f)
        return data.get("Light"), data.get("LightInDark")
    except:
        return None, None

def get_remote_version(owner, repo):
    url = f"https://api.github.com/repos/{owner}/{repo}/releases/latest"
    try:
        context = ssl._create_unverified_context()
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(req, context=context, timeout=10) as resp:
            data = json.loads(resp.read().decode("utf-8"))
            tag = data.get("tag_name", "")
            if tag.startswith("v"):
                tag = tag[1:]
            return tag
    except Exception:
        return None

def version_compare(v1, v2):
    parts1 = [int(x) for x in v1.split(".")]
    parts2 = [int(x) for x in v2.split(".")]
    for a, b in zip(parts1, parts2):
        if a != b:
            return 1 if a > b else -1
    return len(parts1) - len(parts2)

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    bepinex_dir = find_bepinex(script_dir)
    if bepinex_dir is None:
        print("not installed")
        return

    plugins_dir = os.path.join(bepinex_dir, "plugins")
    if not os.path.isdir(plugins_dir):
        print("not installed")
        return

    dll1 = os.path.join(plugins_dir, "Light.dll")
    dll2 = os.path.join(plugins_dir, "LightInDark.dll")
    if not (os.path.isfile(dll1) and os.path.isfile(dll2)):
        print("not installed")
        return

    light_ver, lid_ver = get_local_versions(plugins_dir)
    if light_ver is None or lid_ver is None:
        print("no need")
        return

    remote_light = get_remote_version(REPO_LIGHT_OWNER, REPO_LIGHT_NAME)
    remote_lid   = get_remote_version(REPO_LID_OWNER, REPO_LID_NAME)
    if remote_light is None or remote_lid is None:
        print("github error")
        return
    try:
        need_update = (version_compare(light_ver, remote_light) < 0) or \
                      (version_compare(lid_ver, remote_lid) < 0)
    except:
        need_update = True

    print("need update" if need_update else "no need")

if __name__ == "__main__":
    main()