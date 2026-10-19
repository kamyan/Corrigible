import Foundation
import AVFoundation
import AppKit
let dir=CommandLine.arguments[1], out=URL(fileURLWithPath:CommandLine.arguments[2]); let frames=Int(CommandLine.arguments[3])!; let fps=Int32(CommandLine.arguments[4])!
if FileManager.default.fileExists(atPath:out.path){try FileManager.default.removeItem(at:out)}
let writer=try AVAssetWriter(outputURL:out,fileType:.mp4)
let input=AVAssetWriterInput(mediaType:.video,outputSettings:[AVVideoCodecKey:AVVideoCodecType.h264,AVVideoWidthKey:1280,AVVideoHeightKey:720,AVVideoCompressionPropertiesKey:[AVVideoAverageBitRateKey:4000000]])
let adaptor=AVAssetWriterInputPixelBufferAdaptor(assetWriterInput:input,sourcePixelBufferAttributes:[kCVPixelBufferPixelFormatTypeKey as String:kCVPixelFormatType_32ARGB,kCVPixelBufferWidthKey as String:1280,kCVPixelBufferHeightKey as String:720])
writer.add(input);guard writer.startWriting() else{fatalError("writer start: \(String(describing:writer.error))")};writer.startSession(atSourceTime:.zero)
for i in 0..<frames {
 while !input.isReadyForMoreMediaData {Thread.sleep(forTimeInterval:0.001)}
 let image=NSImage(contentsOfFile:String(format:"%@/%04d.png",dir,i))!;var rect=CGRect(x:0,y:0,width:1280,height:720);let cg=image.cgImage(forProposedRect:&rect,context:nil,hints:nil)!
 var buffer:CVPixelBuffer?;CVPixelBufferPoolCreatePixelBuffer(nil,adaptor.pixelBufferPool!,&buffer);let b=buffer!;CVPixelBufferLockBaseAddress(b,[])
 let context=CGContext(data:CVPixelBufferGetBaseAddress(b),width:1280,height:720,bitsPerComponent:8,bytesPerRow:CVPixelBufferGetBytesPerRow(b),space:CGColorSpaceCreateDeviceRGB(),bitmapInfo:CGImageAlphaInfo.noneSkipFirst.rawValue)!
 context.draw(cg,in:rect);CVPixelBufferUnlockBaseAddress(b,[]);if !adaptor.append(b,withPresentationTime:CMTime(value:Int64(i),timescale:fps)){fatalError("append: \(String(describing:writer.error))")}
}
input.markAsFinished();let done=DispatchSemaphore(value:0);writer.finishWriting{done.signal()};done.wait();guard writer.status == .completed else{fatalError("\(String(describing:writer.error))")};print("\(frames) frames, \(Double(frames)/Double(fps))s → \(out.path)")

let asset=AVURLAsset(url:out);let duration=try await asset.load(.duration).seconds
let reportDir=out.deletingLastPathComponent().deletingLastPathComponent().appendingPathComponent("F6")
let generator=AVAssetImageGenerator(asset:asset);generator.appliesPreferredTrackTransform=true;generator.maximumSize=CGSize(width:960,height:540)
for (i,t) in [0.75,4.366666,7.5].enumerated(){let result=try await generator.image(at:CMTime(seconds:t,preferredTimescale:600));let rep=NSBitmapImageRep(cgImage:result.image);try rep.representation(using:.png,properties:[:])!.write(to:reportDir.appendingPathComponent("\(out.deletingPathExtension().lastPathComponent)_\(i).png"))}
let info:[String:Any]=["seconds":duration,"frames":frames,"fps":Int(fps),"width":1280,"height":720]
try JSONSerialization.data(withJSONObject:info,options:[.prettyPrinted,.sortedKeys]).write(to:reportDir.appendingPathComponent("\(out.deletingPathExtension().lastPathComponent)-info.json"))
print(info)
