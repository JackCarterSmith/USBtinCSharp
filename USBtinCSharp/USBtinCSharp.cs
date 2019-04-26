using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace USBtinCSharp
{
    public class USBtinCSharp
    {
        /** Serial port (virtual) to which USBtin is connected */
        protected SerialPort _serialPort;

        /** Characters coming from USBtin are collected in this stringBuilder */
        protected stringBuilder incomingMessage = new stringBuilder();

        /** Listener for CAN messages */
        protected List<CANMessageListener> listeners = new List<CANMessageListener>();

        /** Transmit fifo */
        protected LinkedList<CANMessage> fifoTX = new LinkedList<CANMessage>();

        /** USBtin firmware version */
        protected string firmwareVersion;

        /** USBtin hardware version */
        protected string hardwareVersion;

        /** USBtin serial number */
        protected string serialNumber;

        /** Timeout for response from USBtin */
        protected readonly int TIMEOUT = 1000;

        public enum OpenMode
        {
            /** Send and receive on CAN bus */
            ACTIVE,
            /** Listen only, sending messages is not possible */
            LISTENONLY,
            /** Loop back the sent CAN messages. Disconnected from physical CAN bus */
            LOOPBACK
        }
    }
}
