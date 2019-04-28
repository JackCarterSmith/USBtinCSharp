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
     * Exception regarding USBtin
     */
    public class USBtinException : Exception
    {
        /**
         * Standard constructor
         */
        public USBtinException() : base() {
        }

        /**
         * Construct exception
         * 
         * @param message Message string
         */
        public USBtinException(string message) : base(message) {
        }

        /**
         * Construct exception
         * 
         * @param message Message string
         * @param cause Cause of exception
         */
        public USBtinException(string message, Exception ex) : base(message, ex) {
        }

        /**
         * Construct exception
         * 
         * @param cause Cause of exception
         */
        public USBtinException(Exception ex, string format, params object[] args) : base(string.Format(format, args), ex) {
        }
    }
}
