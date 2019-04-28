using System;
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
     * Represents a CAN message.
     */
    public class CANMessage
    {
        /** CAN message ID */
        protected int id;

        /** CAN message payload data */
        protected byte[] data;

        /** Marks frames with extended message id */
        protected bool extended;

        /** Marks request for transmition frames */
        protected bool rtr;

        /**
         * <summary>
         * Get CAN message identifier
         * </summary>
         * 
         * <returns>CAN message identifier</returns>
         */
        public int GetId()
        {
            return id;
        }

        /**
         * <summary>
         * Set CAN message identifier
         * </summary>
         * 
         * <param name="id">CAN message identifier</param>
         */
        public void SetId(int id)
        {

            if (id > (0x1fffffff))
                id = 0x1fffffff;

            if (id > 0x7ff)
                extended = true;

            this.id = id;
        }

        /**
         * <summary>
         * Get CAN message payload data
         * </summary>
         * 
         * <returns>CAN message payload data</returns>
         */
        public byte[] GetData()
        {
            return data;
        }

        /**
         * <summary>
         * Set CAN message payload data
         * </summary>
         * 
         * <param name="data"></param> 
         */
        public void SetData(byte[] data)
        {
            this.data = data;
        }

        /**
         * <summary>
         * Determine if CAN message id is extended
         * </summary>
         * 
         * <returns>true if extended CAN message</returns>
         */
        public bool IsExtended()
        {
            return extended;
        }

        /**
         * <summary>
         * Determine if CAN message is a request for transmission
         * </summary>
         * 
         * <returns>true if RTR message</returns>
         */
        public bool IsRtr()
        {
            return rtr;
        }

        /**
         * <summary>
         * Create message with given id and data.
         * Depending on Id, the extended flag is set.
         * </summary>
         * 
         * <param name="id">Message identifier</param>
         * <param name="data">Payload data</param>
         */
        public CANMessage(int id, byte[] data)
        {
            this.data = data;
            this.extended = false;
            SetId(id);
            this.rtr = false;
        }

        /**
         * <summary>
         * Create message with given message properties.
         * </summary>
         * 
         * <param name="id">Message identifier</param>
         * <param name="data">Payload data</param>
         * <param name="extended">Marks messages with extended identifier</param>
         * <param name="rtr">Marks RTR messages</param>
         */
        public CANMessage(int id, byte[] data, bool extended, bool rtr)
        {
            SetId(id);
            this.data = data;
            this.extended = extended;
            this.rtr = rtr;
        }

        /**
         * <summary>
         * Create message with given message string.
         * The message string is parsed. On errors, the corresponding value is
         * set to zero. 
         * 
         * Example message strings:
         * t1230        id: 123h        dlc: 0      data: --
         * t00121122    id: 001h        dlc: 2      data: 11 22
         * T12345678197 id: 12345678h   dlc: 1      data: 97
         * r0037        id: 003h        dlc: 7      RTR
         * </summary>
         * 
         * <param name="msg">Message string</param>
         */
        public CANMessage(string msg)
        {

            this.rtr = false;
            int index = 1;
            char type;
            if (msg.Length > 0) type = msg[0];
            else type = 't';

            switch (type) {
                case 'r':
                    this.rtr = true;
                    goto default;
                default:
                case 't':
                    try {
                        this.id = int.Parse(msg.Substring(index, index + 3), System.Globalization.NumberStyles.AllowParentheses);
                    }
                    catch (Exception) {
                        this.id = 0;
                    }
                    this.extended = false;
                    index += 3;
                    break;
                case 'R':
                    this.rtr = true;
                    goto case 't';
                case 'T':
                    try {
                        this.id = int.Parse(msg.Substring(index, index + 8), System.Globalization.NumberStyles.AllowParentheses);
                    }
                    catch (Exception) {
                        this.id = 0;
                    }
                    this.extended = true;
                    index += 8;
                    break;
            }

            int length;
            try {
                length = int.Parse(msg.Substring(index, index + 1), System.Globalization.NumberStyles.AllowParentheses);
                if (length > 8) length = 8;
            }
            catch (Exception) {
                length = 0;
            }
            index += 1;

            this.data = new byte[length];
            if (!this.rtr) {
                for (int i = 0; i < length; i++) {
                    try {
                        this.data[i] = (byte)int.Parse(msg.Substring(index, index + 2), System.Globalization.NumberStyles.AllowParentheses);
                    }
                    catch (Exception) {
                        this.data[i] = 0;
                    }
                    index += 2;
                }
            }
        }

        /**
         * <summary>
         * Get string representation of CAN message
         * </summary>
         * 
         * <returns>CAN message as string representation</returns>
         */
        public override string ToString()
        {
            string s;
            if (this.extended)
            {
                if (this.rtr) s = "R";
                else s = "T";
                s = s + string.Format("%08x", this.id);
            }
            else
            {
                if (this.rtr) s = "r";
                else s = "t";
                s = s + string.Format("%03x", this.id);
            }
            s = s + string.Format("%01x", this.data.Length);

            if (!this.rtr)
            {
                for (int i = 0; i < this.data.Length; i++)
                {
                    s = s + string.Format("%02x", this.data[i]);
                }
            }
            return s;
        }
    }
}
