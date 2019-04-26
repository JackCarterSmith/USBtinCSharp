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

        /** Characters coming from USBtin are collected in this StringBuilder */
        protected StringBuilder incomingMessage = new StringBuilder();

        /** Listener for CAN messages */
        protected ArrayList<CANMessageListener> listeners = new ArrayList<CANMessageListener>();

        /** Transmit fifo */
        protected LinkedList<CANMessage> fifoTX = new LinkedList<CANMessage>();

        /** USBtin firmware version */
        protected String firmwareVersion;

        /** USBtin hardware version */
        protected String hardwareVersion;

        /** USBtin serial number */
        protected String serialNumber;

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
