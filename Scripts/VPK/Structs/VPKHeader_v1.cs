struct VPKHeader_v1
{
	const uint Signature = 0x55aa1234;
	const uint Version = 1;
 
	// The size, in bytes, of the directory tree
	uint TreeSize;
};