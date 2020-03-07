#include <iostream>
#include <oleidl.h>
#include <comdef.h>
#include <vector>
#include <fstream>

using namespace std;

string getClipboardText()
{
	string ClipBoardText;
	//判断剪贴板的数据格式是否可以处理。  
	if (!IsClipboardFormatAvailable(CF_TEXT))
		return ClipBoardText;

	//打开剪贴板。          
	if (!OpenClipboard(NULL))
		return ClipBoardText;

	//获取数据  
	HANDLE hMem = GetClipboardData(CF_TEXT);
	if (hMem != NULL)
	{
		//获取字符串。  
		LPSTR lpStr = (LPSTR)GlobalLock(hMem);
		if (lpStr != NULL)
		{
			ClipBoardText = lpStr;
			//释放锁内存  
			GlobalUnlock(hMem);
		}
	}
	//关闭剪贴板        
	CloseClipboard();
	return ClipBoardText;
}

string& replace_all(string& str, const   string& old_value, const   string& new_value)
{
	while (true) {
		string::size_type   pos(0);
		if ((pos = str.find(old_value)) != string::npos)
			str.replace(pos, old_value.length(), new_value);
		else   break;
	}
	return   str;
}

int main(int argc, char* argv[])
{
    string text = getClipboardText();
	replace_all(text, "\r\n", "\n");
	string file = argv[1];
	ofstream fs(file);
	fs << text;
	fs.close();
	return 0;
}
