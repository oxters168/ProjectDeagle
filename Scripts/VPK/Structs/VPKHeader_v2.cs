struct VPKHeader_v2
{
	const uint Signature = 0x55aa1234;
	const uint Version = 2;
 
	// The size, in bytes, of the directory tree
	uint TreeSize;
 
	// How many bytes of file content are stored in this VPK file (0 in CSGO)
	uint FileDataSectionSize;
 
	// The size, in bytes, of the section containing MD5 checksums for external archive content
	uint ArchiveMD5SectionSize;
 
	// The size, in bytes, of the section containing MD5 checksums for content in this file (should always be 48)
	uint OtherMD5SectionSize;
 
	// The size, in bytes, of the section containing the public key and signature. This is either 0 (CSGO & The Ship) or 296 (HL2, HL2:DM, HL2:EP1, HL2:EP2, HL2:LC, TF2, DOD:S & CS:S)
	uint SignatureSectionSize;
};