﻿using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * This file is part of USBtinCSharp.
 *
 * Copyright (C) 2019   JackCarterSmith  (Originaly by Thomas Fischl)
 *
 * USBtinLib is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * USBtinLib is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with USBtinCShar.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace USBtinCSharp
{
    /**
     * <summary>
     * Represents an USBtin device.
     * Provide access to an USBtin device (http://www.fischl.de/usbtin) over virtual
     * serial port (CDC).
     * </summary>
     */
    public class USBtinCSharp
    {
        /** Serial port (virtual) to which USBtin is connected */
        protected SerialPort serialPort;

        /** Characters coming from USBtin are collected in this stringBuilder */
        protected StringBuilder incomingMessage = new StringBuilder();

        /** Listener for CAN messages */
        protected List<ICANMessageListener> listeners = new List<ICANMessageListener>();

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

        /** Bytes array decoder/encoder */
        private ASCIIEncoding encoding = new ASCIIEncoding();

        public enum OpenMode
        {
            /** Send and receive on CAN bus */
            ACTIVE,
            /** Listen only, sending messages is not possible */
            LISTENONLY,
            /** Loop back the sent CAN messages. Disconnected from physical CAN bus */
            LOOPBACK
        }

        /**
         * <summary>
         * Get firmware version string.
         * During connect() the firmware version is requested from USBtin.
         * </summary>
         * 
         * <returns>Firmware version</returns>
         */
        public string GetFirmwareVersion()
        {
            return firmwareVersion;
        }

        /**
         * <summary>
         * Get hardware version string.
         * During connect() the hardware version is requested from USBtin.
         * </summary>
         * 
         * <returns>Hardware version</returns>
         */
        public string GetHardwareVersion()
        {
            return hardwareVersion;
        }

        /**
         * <summary>
         * Get serial number string.
         * During connect() the serial number is requested from USBtin.
         * </summary>
         *
         * <returns>Serial number</returns>
         */
        public string GetSerialNumber()
        {
            return serialNumber;
        }

        /**
         * <summary>
         * Get list of serial devices connected.
         * </summary>
         *
         * <returns>List of serial devices</returns>
         */
        public List<string> GetDevives()
        {
            return SerialPort.GetPortNames().ToList();
        }

        /**
         * <summary>
         * Connect to USBtin on given port.
         * Opens the serial port, clears pending characters and send close command
         * to make sure that we are in configuration mode.
         * </summary>
         * 
         * <param name="portName">Name of virtual serial port</param>
         * @throws USBtinException Error while connecting to USBtin
         */
        public void Connect(string portName)
        {
            byte[] buffer = null;

            try
            {
                // create serial port object
                serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.None)
                {
                    ReadTimeout = TIMEOUT,
                    WriteTimeout = TIMEOUT
                };

                // open serial port and initialize it
                serialPort.Open();

                // clear port and make sure we are in configuration mode (close cmd)
                serialPort.Write(encoding.GetBytes("\rC\r"), 0, 1);
                Thread.Sleep(100);
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.Write(encoding.GetBytes("C\r"), 0, 1);
                byte b;
                do
                {
                    serialPort.Read(buffer, 0, 1);
                    b = buffer[0];
                } while ((b != '\r') && (b != 7));

                // get version strings
                this.firmwareVersion = this.Transmit("v").Substring(1);
                this.hardwareVersion = this.Transmit("V").Substring(1);
                this.serialNumber = this.Transmit("N").Substring(1);

                // reset overflow error flags
                this.Transmit("W2D00");

            }
            catch (Exception e)
            {
                throw new USBtinException(e.GetType().ToString());
            }
        }

        /**
        * Disconnect.
        * Close serial port connection
        * 
        * @throws USBtinException Error while closing connection
        */
        public void Disconnect()
        {
            try
            {
                serialPort.Close();
            }
            catch (Exception e)
            {
                throw new USBtinException(e.Message);
            }
        }

        /**
         * Open CAN channel.
         * Set given baudrate and open the CAN channel in given mode.
         * 
         * @param baudrate Baudrate in bits/second
         * @param mode CAN bus accessing mode
         * @throws USBtinException Error while opening CAN channel
         */
        public void OpenCANChannel(int baudrate, OpenMode mode)
        {
            try
            {
                // set baudrate
                char baudCh = ' ';
                switch (baudrate)
                {
                    case 10000: baudCh = '0'; break;
                    case 20000: baudCh = '1'; break;
                    case 50000: baudCh = '2'; break;
                    case 100000: baudCh = '3'; break;
                    case 125000: baudCh = '4'; break;
                    case 250000: baudCh = '5'; break;
                    case 500000: baudCh = '6'; break;
                    case 800000: baudCh = '7'; break;
                    case 1000000: baudCh = '8'; break;
                }

                if (baudCh != ' ')
                {
                    // use preset baudrate
                    this.Transmit("S" + baudCh);
                }
                else
                {
                    // calculate baudrate register settings

                    const int FOSC = 24000000;
                    int xdesired = FOSC / baudrate;
                    int xopt = 0;
                    int diffopt = 0;
                    int brpopt = 0;

                    // walk through possible can bit length (in TQ)
                    for (int x = 11; x <= 23; x++)
                    {
                        // get next even value for baudrate factor
                        int xbrp = (xdesired * 10) / x;
                        int m = xbrp % 20;
                        if (m >= 10) xbrp += 20;
                        xbrp -= m;
                        xbrp /= 10;

                        // check bounds
                        if (xbrp < 2) xbrp = 2;
                        if (xbrp > 128) xbrp = 128;

                        // calculate diff
                        int xist = x * xbrp;
                        int diff = xdesired - xist;
                        if (diff < 0) diff = -diff;

                        // use this clock option if it is better than previous
                        if ((xopt == 0) || (diff <= diffopt)) { xopt = x; diffopt = diff; brpopt = xbrp / 2 - 1; };
                    }

                    // mapping for CNF register values
                    int[] cnfvalues = { 0x9203, 0x9303, 0x9B03, 0x9B04, 0x9C04, 0xA404, 0xA405, 0xAC05, 0xAC06, 0xAD06, 0xB506, 0xB507, 0xBD07 };

                    Transmit("s" + string.Format("%02x", brpopt | 0xC0) + string.Format("%04x", cnfvalues[xopt - 11]));

                    //System.out.println("No preset for given baudrate " + baudrate + ". Set baudrate to " + (FOSC / ((brpopt + 1) * 2) / xopt));
                    throw new USBtinException("No preset for given baudrate " + baudrate + ". Set baudrate to " + (FOSC / ((brpopt + 1) * 2) / xopt));
                }

                // open can channel
                char modeCh;
                switch (mode)
                {
                    case OpenMode.LISTENONLY: modeCh = 'L'; break;
                    case OpenMode.LOOPBACK: modeCh = 'l'; break;
                    case OpenMode.ACTIVE: modeCh = 'O'; break;
                    default: throw new USBtinException("Mode " + mode + " not supported. Opening listen only.");
                }
                Transmit(modeCh + "");

                // register serial port event listener
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            }
            catch (Exception e)
            {
                throw new USBtinException(e.Message);
            }
        }

        /**
         * Close CAN channel.
         * 
         * @throws USBtinException Error while closing CAN channel
         */
        public void CloseCANChannel()
        {
            try
            {
                //Remove event listener from serial port
                serialPort.DataReceived -= new SerialDataReceivedEventHandler(DataReceivedHandler);
                serialPort.Write(encoding.GetBytes("C\r"), 0, 1);
            }
            catch (Exception e)
            {
                throw new USBtinException(e.Message);
            }

            firmwareVersion = null;
            hardwareVersion = null;
        }

        /**
         * <summary>
         * Handle serial port event.
         * Read single byte and check for end of line character.
         * If end of line is reached, parse command and dispatch it.
         * </summary>
         * 
         * <param name="e">Serial port event args</param>
         */
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort s = (SerialPort)sender;
            try
            {
                byte[] buffer = null;
                s.Read(buffer, 0, s.BytesToRead);
                foreach (byte b in buffer)
                {
                    if ((b == '\r') & incomingMessage.Length > 0)
                    {
                        string message = incomingMessage.ToString();
                        char cmd = message[0];

                        // check if this is a CAN message
                        if (cmd == 't' || cmd == 'T' || cmd == 'r' || cmd == 'R')
                        {
                            // create CAN message from message string
                            CANMessage canmsg = new CANMessage(message);

                            // give the CAN message to the listeners
                            foreach (ICANMessageListener listener in listeners)
                            {
                                listener.ReceiveCANMessage(canmsg);
                            }
                        }
                        else if ((cmd == 'z') || (cmd == 'Z'))
                        {
                            // remove first message from transmit fifo and send next one
                            fifoTX.RemoveFirst();

                            try
                            {
                                SendFirstTXFifoMessage();
                            }
                            catch (USBtinException)
                            {
                                //System.err.println(ex);
                            }
                        }
                        incomingMessage.Length = 0;
                    }
                    else if (b == 0x07)
                    {
                        // resend first element from tx fifo
                        try
                        {
                            SendFirstTXFifoMessage();
                        }
                        catch (USBtinException)
                        {
                            //System.err.println(ex);
                        }
                    }
                    else if (b != '\r')
                    {
                        incomingMessage.Append((char)b);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new USBtinException(ex.Message);
            }
        }

        /**
         * <summary>
         * Read response from USBtin
         * </summary>
         *
         * <returns>Response from USBtin</returns>
         * @throws SerialPortException Error while accessing USBtin
         * @throws SerialPortTimeoutException Timeout of serial port
         */
        private string ReadResponse()
        {
            StringBuilder response = new StringBuilder();
            byte[] buffer = null;
            while (true)
            {
                serialPort.Read(buffer, 0, serialPort.BytesToRead);
                if (buffer[0] == '\r')
                {
                    return response.ToString();
                }
                else if (buffer[0] == 7)
                {
                    throw new USBtinException(serialPort.PortName + " - BELL signal");
                }
                else
                {
                    response.Append((char)buffer[0]);
                }
            }
        }

        /**
         * <summary>
         * Transmit given command to USBtin
         * </summary>
         *
         * <param name="cmd">Command</param>
         * <returns>Response from USBtin</returns>
         * @throws SerialPortException Error while talking to USBtin
         * @throws SerialPortTimeoutException Timeout of serial port
         */
        private string Transmit(string cmd)
        {
            string cmdline = cmd + "\r";
            serialPort.Write(encoding.GetBytes(cmdline), 0, 1);

            return ReadResponse();
        }

        /**
         * <summary>
         * Add message listener
         * </summary>
         * 
         * <param name="listener">Listener object</param>
         */
        public void AddMessageListener(ICANMessageListener listener)
        {
            listeners.Add(listener);
        }

        /**
         * <summary>
         * Remove message listener.
         * </summary>
         * 
         * <param name="listener">Listener object</param>
         */
        public void RemoveMessageListener(ICANMessageListener listener)
        {
            listeners.Remove(listener);
        }

        /**
         * <summary>
         * Send first message in tx fifo
         * </summary>
         * 
         * @throws USBtinException On serial port errors
         */
        protected void SendFirstTXFifoMessage()
        {
            if (fifoTX.Count == 0)
            {
                return;
            }

            CANMessage canmsg = fifoTX.First.Value;

            try
            {
                serialPort.Write(encoding.GetBytes(canmsg.ToString() + "\r"), 0, 1);
            }
            catch (Exception e)
            {
                throw new USBtinException(e.Message);
            }
        }

        /**
         * <summary>
         * Send given can message.
         * </summary>
         * 
         * <param name="canmsg">Can message to send</param>
         * @throws USBtinException  On serial port errors
         */
        public void Send(CANMessage canmsg)
        {
            fifoTX.AddLast(canmsg);

            if (fifoTX.Count > 1) return;

            SendFirstTXFifoMessage();
        }

        /**
         * <summary>
        * Write given register of MCP2515
        * </summary>
        * 
        * <param name="register">Register address</param>
        * <param name="value">Value to write</param>
        * @throws USBtinException On serial port errors
        */
        public void WriteMCPRegister(int register, byte value)
        {
            try
            {
                string cmd = "W" + string.Format("%02x", register) + string.Format("%02x", value);
                Transmit(cmd);
            }
            catch (Exception e)
            {
                throw new USBtinException(e.Message);
            }
        }

        /**
         * <summary>
         * Write given mask registers to MCP2515
         * </summary>
         * 
         * <param name="maskid">Mask identifier (0 = RXM0, 1 = RXM1)</param>
         * <param name="registers">Register values to write</param>
         * @throws USBtinException On serial port errors
         */
        protected void WriteMCPFilterMaskRegisters(int maskid, byte[] registers)
        {
            for (int i = 0; i < 4; i++)
            {
                WriteMCPRegister(0x20 + maskid * 4 + i, registers[i]);
            }
        }

        /**
         * <summary>
         * Write given filter registers to MCP2515
         * </summary>
         * 
         * <param name="filterid">Filter identifier (0 = RXF0, ... 5 = RXF5)</param>
         * <param name="registers">Register values to write</param>
         * @throws USBtinException On serial port errors
         */
        protected void WriteMCPFilterRegisters(int filterid, byte[] registers)
        {
            int[] startregister = { 0x00, 0x04, 0x08, 0x10, 0x14, 0x18 };

            for (int i = 0; i < 4; i++)
            {
                WriteMCPRegister(startregister[filterid] + i, registers[i]);
            }
        }

        /**
         * <summary>
         * Set hardware filters.
         * Call this function after connect() and before openCANChannel()!
         * </summary>
         * 
         * <param name="fc">Filter chains (USBtin supports maximum 2 hardware filter chains)</param>
         * @throws USBtinException On serial port errors
         */
        public void SetFilter(FilterChain[] fc)
        {
            /*
                * The MCP2515 offers two filter chains. Each chain consists of one mask
                * and a set of filters:
                * 
                * RXM0         RXM1
                *   |            |
                * RXF0         RXF2
                * RXF1         RXF3
                *              RXF4
                *              RXF5
                */

            // if no filter chain given, accept all messages
            if ((fc == null) || (fc.Length == 0))
            {
                byte[] registers = { 0, 0, 0, 0 };
                WriteMCPFilterMaskRegisters(0, registers);
                WriteMCPFilterMaskRegisters(1, registers);

                return;
            }

            // check maximum filter channels
            if (fc.Length > 2)
            {
                throw new USBtinException("Too many filter chains: " + fc.Length + " (maximum is 2)!");
            }

            // swap channels if necessary and check filter chain length
            if (fc.Length == 2)
            {
                if (fc[0].getFilters().length > fc[1].getFilters().length)
                {
                    FilterChain temp = fc[0];
                    fc[0] = fc[1];
                    fc[1] = temp;
                }

                if ((fc[0].getFilters().length > 2) || (fc[1].getFilters().length > 4))
                {
                    throw new USBtinException("Filter chain too long: " + fc[0].getFilters().length + "/" + fc[1].getFilters().length + " (maximum is 2/4)!");
                }
            }
            else if (fc.Length == 1)
            {
                if ((fc[0].getFilters().length > 4))
                {
                    throw new USBtinException("Filter chain too long: " + fc[0].getFilters().length + " (maximum is 4)!");
                }
            }

            // set MCP2515 filter/mask registers; walk through filter channels
            int filterid = 0;
            int fcidx = 0;
            for (int channel = 0; channel < 2; channel++)
            {

                // set mask
                WriteMCPFilterMaskRegisters(channel, fc[fcidx].getMask().getRegisters());

                // set filters
                byte[] registers = { 0, 0, 0, 0 };
                for (int i = 0; i < (channel == 0 ? 2 : 4); i++)
                {
                    if (fc[fcidx].getFilters().length > i)
                    {
                        registers = fc[fcidx].getFilters()[i].getRegisters();
                    }

                    WriteMCPFilterRegisters(filterid, registers);
                    filterid++;
                }

                // go to next filter chain if available
                if (fc.Length - 1 > fcidx)
                {
                    fcidx++;
                }
            }
        }
    }
}
