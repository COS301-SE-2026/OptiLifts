import { SpiderGraph } from '@/components/ui/spider-graph'                         
                                                                                       
    export default function SchedulePage() {
      const myMuscleVolumes = {                                                        
        Chest: 10,                                                                     
        Back: 15,                                                                       
        Shoulders: 12,                                                                 
        Arms: 6,                                                                       
        Legs: 14,                                                                      
        Core: 4,                                                                       
      }                                                                                
                                                                                       
      return (                                                                         
        <div className="p-6 max-w-md bg-card border border-border rounded-xl">         
          <h2 className="text-xl font-bold mb-4">Muscle Balance Chart</h2>             
          <SpiderGraph data={myMuscleVolumes} />                                       
        </div>                                                                         
      )                                                                                
    } 