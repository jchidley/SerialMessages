SerialMessages
==============

Light messaging using serial.

Open port, set up handler for received data, send stuff 

### ToDo: lots!

convert to library for use by other programs

Include new fields, all 1 byte to save space
* error checking
* length field
* source, destination fields (for Easy Radio http://www.lprs.co.uk/easy-radio/what-is-it-and-why-use-it.html)
* resize buffer to 256 bytes (to fit with length)
