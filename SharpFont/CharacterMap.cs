﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpFont {
    class CharacterMap {
        Dictionary<CodePoint, int> table = new Dictionary<CodePoint, int>();

        CharacterMap (Dictionary<CodePoint, int> table) {
            this.table = table;
        }

        public static CharacterMap ReadCmap (DataReader reader) {
            // skip version
            var cmapOffset = reader.Position;
            reader.Skip(sizeof(short));

            // read all of the subtable headers
            var subtableCount = reader.ReadUInt16BE();
            var subtableHeaders = new CmapSubtableHeader[subtableCount];
            for (int i = 0; i < subtableHeaders.Length; i++) {
                subtableHeaders[i] = new CmapSubtableHeader {
                    PlatformID = reader.ReadUInt16BE(),
                    EncodingID = reader.ReadUInt16BE(),
                    Offset = reader.ReadUInt32BE()
                };
            }

            // search for a "full" Unicode table first
            var chosenSubtableOffset = 0u;
            for (int i = 0; i < subtableHeaders.Length; i++) {
                var platform = subtableHeaders[i].PlatformID;
                var encoding = subtableHeaders[i].EncodingID;
                if ((platform == CmapPlatform.Microsoft && encoding == MicrosoftCmapEncoding.UnicodeFull) ||
                    (platform == CmapPlatform.Unicode && encoding == UnicodeCmapEncoding.Unicode32)) {

                    chosenSubtableOffset = subtableHeaders[i].Offset;
                    break;
                }
            }

            // if no full unicode table, so just grab the first
            // one that supports any flavor of Unicode
            if (chosenSubtableOffset == 0) {
                for (int i = 0; i < subtableHeaders.Length; i++) {
                    var platform = subtableHeaders[i].PlatformID;
                    var encoding = subtableHeaders[i].EncodingID;
                    if ((platform == CmapPlatform.Microsoft && encoding == MicrosoftCmapEncoding.UnicodeBmp) ||
                         platform == CmapPlatform.Unicode) {

                        chosenSubtableOffset = subtableHeaders[i].Offset;
                        break;
                    }
                }
            }

            // no unicode support at all is an error
            if (chosenSubtableOffset == 0)
                throw new InvalidFontException("Font does not support Unicode.");

            // jump to our chosen table and find out what format it's in
            reader.Seek(cmapOffset + chosenSubtableOffset);
            var format = reader.ReadUInt16BE();
            switch (format) {
                case 4: return ReadCmapFormat4(reader);
                default: throw new InvalidFontException("Unsupported cmap format.");
            }
        }

        unsafe static CharacterMap ReadCmapFormat4 (DataReader reader) {
            // skip over length and language
            reader.Skip(sizeof(short) * 2);

            // figure out how many segments we have
            var segmentCount = reader.ReadUInt16BE() / 2;
            if (segmentCount > MaxSegments)
                throw new InvalidFontException("Too many cmap segments.");

            // skip over searchRange, entrySelector, and rangeShift
            reader.Skip(sizeof(short) * 3);

            // read in segment ranges
            var endCount = stackalloc int[segmentCount];
            for (int i = 0; i < segmentCount; i++)
                endCount[i] = reader.ReadUInt16BE();

            reader.Skip(sizeof(short));     // padding

            var startCount = stackalloc int[segmentCount];
            for (int i = 0; i < segmentCount; i++)
                startCount[i] = reader.ReadUInt16BE();

            var idDelta = stackalloc int[segmentCount];
            for (int i = 0; i < segmentCount; i++)
                idDelta[i] = reader.ReadInt16BE();

            // build table from each segment
            var table = new Dictionary<CodePoint, int>();
            for (int i = 0; i < segmentCount; i++) {
                // read the "idRangeOffset" for the current segment
                // if nonzero, we need to jump into the glyphIdArray to figure out the mapping
                // the layout is bizarre; see the OpenType spec for details
                var idRangeOffset = reader.ReadUInt16BE();
                if (idRangeOffset != 0) {
                    var currentOffset = reader.Position;
                    reader.Seek(currentOffset + idRangeOffset);

                    var end = endCount[i];
                    var delta = idDelta[i];
                    for (var codepoint = startCount[i]; codepoint <= end; codepoint++) {
                        var glyphId = reader.ReadUInt16BE();
                        if (glyphId != 0) {
                            var glyphIndex = (glyphId + delta) & 0xFFFF;
                            if (glyphIndex != 0)
                                table.Add((CodePoint)codepoint, glyphIndex);
                        }
                    }

                    reader.Seek(currentOffset);
                }
                else {
                    // otherwise, do a straight iteration through the segment
                    var end = endCount[i];
                    var delta = idDelta[i];
                    for (var codepoint = startCount[i]; codepoint <= end; codepoint++) {
                        var glyphIndex = (codepoint + delta) & 0xFFFF;
                        if (glyphIndex != 0)
                            table.Add((CodePoint)codepoint, glyphIndex);
                    }
                }
            }

            return new CharacterMap(table);
        }

        const int MaxSegments = 1024;

        static class CmapPlatform {
            public const int Unicode = 0;
            public const int Microsoft = 3;
        }

        static class MicrosoftCmapEncoding {
            public const int UnicodeBmp = 1;
            public const int UnicodeFull = 10;
        }

        static class UnicodeCmapEncoding {
            public const int Unicode32 = 4;
        }

        struct CmapSubtableHeader {
            public int PlatformID;
            public int EncodingID;
            public uint Offset;
        }
    }
}