#ifndef _ANDROID_SERIAL_PORT_H
#define _ANDROID_SERIAL_PORT_H

#include <jni.h>

#ifdef __cplusplus
extern "C" {
#endif
	JNIEXPORT int MyTestFunction(int x);

	JNIEXPORT int OpenSerialPort(char* path, int baudrate, int flags);

	JNIEXPORT void CloseSerialPort(int handle);

#ifdef __cplusplus
}
#endif

#endif // !_ANDROID_SERIAL_PORT_H






