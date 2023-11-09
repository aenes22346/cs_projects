# -*- coding: utf-8 -*-

import matplotlib.pyplot as plt
import matplotlib
import pandas as pd
import numpy as np
import networkx as nx
import matplotlib.colors as mcolors
import math
import copy

COLORS = list(matplotlib.colors.cnames.values())

COLORS2 = mcolors.BASE_COLORS
mcolors.BASE_COLORS.pop('w')
COLORS2 = list(COLORS2.values())

SPEED = 1.0
ORIGIN = 0
CAP = 79.69
RATE = 1.0

# Haversine distance function
def haversine(lat1, lon1, lat2, lon2):

    # Convert decimal degrees to radians
    lon1, lat1, lon2, lat2 = map(math.radians, [lon1, lat1, lon2, lat2])

    # Haversine formula
    dlon = lon2 - lon1
    dlat = lat2 - lat1
    a = math.sin(dlat/2)**2 + math.cos(lat1) * math.cos(lat2) * math.sin(dlon/2)**2
    c = 2 * math.asin(math.sqrt(a))

    # Earth's radius in kilometers
    r = 6371
    return c * r

# Euclidean distance function
def euclidean(lat1, lon1, lat2, lon2):
   
    return math.sqrt( (lat1 -lat2)*(lat1 -lat2) + (lon1 - lon2)*(lon1 - lon2))

# Function for checking if route suits time windows
def checkTimeWindow(left, right, current_tour, earliest, latest, dis, service_time):
    
    tour = copy.copy(current_tour)
    if right == tour[1]:
        tour.insert(1, left)
    elif left == tour[-2]:
        tour.insert(-1, right)

    current_time = 0

    for i in range(1 , len(tour)):

        time = (dis[tour[i-1]][tour[i]] / SPEED) + service_time[tour[i-1]] + current_time
        if earliest[tour[i]] <= time <= latest[tour[i]]:
            current_time += (dis[tour[i-1]][tour[i]] / SPEED) + service_time[tour[i-1]]
        else:
            return False

    return True

# Function for printing time windows of route
def printTimeWindow(tour, dis, service_time, earliest, latest):
    current_time = 0

    for i in range(1 , len(tour)):
        current_time += (dis[tour[i-1]][tour[i]] / SPEED) + service_time[tour[i-1]]
        print(f"Node: {tour[i]} , Current Time: {current_time}, Earliest: {earliest[tour[i]]}, Latest: {latest[tour[i]]}")


# Another Function for checking if route suits time windows
def checkTimeWindow2(tour, dis, service_time, earliest, latest):

    current_time = 0
    for i in range(1 , len(tour)):

        time = current_time + service_time[ tour[i-1] ] + (dis[tour[i-1]][tour[i]] / SPEED)

        if earliest[tour[i]] <= time <= latest[tour[i]]:
            current_time += (dis[tour[i-1]][tour[i]] / SPEED) + service_time[tour[i-1]]
        else:
            return False

    return True

def checkTimeWindow2_waiting(tour, dis, service_time, earliest, latest):

    current_time = 0
    for i in range(1 , len(tour)):

        time = current_time + service_time[ tour[i-1] ] + (dis[tour[i-1]][tour[i]] / SPEED)

        if i != len(tour)-1:
            if time <= earliest[tour[i]]:
                current_time = earliest[tour[i]] + service_time[tour[i-1]]
            else:
                return False
            
        elif i == len(tour)-1:
            if time <= latest[tour[i]]:
                current_time += (dist[i][i-1]/SPEED) + service_time[tour[i-1]]
            else:
                return False
            
    current_time = 0

    for i in range(1 , len(tour)):

        time = current_time + service_time[ tour[i-1] ] + (dis[tour[i-1]][tour[i]] / SPEED)

        if i != len(tour)-1:
            if time <= earliest[tour[i]]:
                current_time = earliest[tour[i]] + service_time[tour[i-1]]
 
        elif i == len(tour)-1:
            if time <= latest[tour[i]]:
                current_time += (dist[i][i-1]/SPEED) + service_time[tour[i-1]]
        
        print(f"Node: {tour[i]}, Current Time: {current_time}, Latest: {latest[tour[i]]}")

    return True   
    
#Main function of Savings Algorithm
def savings(nodes, origin, dist, cap, earliest, latest, service_time):


    # Customer nodes except origin
    customers = {i for i in nodes if i != origin}
    
    # Savings computation
    savings = {(i, j): round(dist[i][origin] + dist[origin][j] - dist[i][j], 3) 
               for i in customers for j in customers if j != i}
          
    # Nodes (i,j) savings decreasing order
    savings = dict(sorted(savings.items(), key=lambda item: item[1], reverse = True))
    visited_nodes = set()
    visited_nodes_2 = set()
    eliminated_nodes = set()
    tours = list()
    counter = 0

    #Check if nodes fits to capacity
    for customer in customers:
        if dist[origin][customer]*2*RATE > cap:
            visited_nodes.add(customer)
            eliminated_nodes.add(customer)

    # MODEL
    while len(visited_nodes) < len(customers) and counter < len(customers)*10:
        counter += 1
        current_tour: list = []
        filled_cap = 0

        if current_tour != []:
            print(current_tour)

        for (left, right) in savings:


            #If both left and right nodes are visited
            if left in visited_nodes and right in visited_nodes:
                pass

            #If right node is visited but left node is not visited            
            elif right in visited_nodes and (left not in current_tour) and current_tour != []:

                indx = 0
                checking = True
                insertList = [1 ,-1]
                while indx < len(insertList) and checking:

                    tempTour = copy.copy(current_tour)
                    tempTour.insert(insertList[indx], left)

                    if checkTimeWindow2(tempTour,dist,service_time,earliest,latest) and insertList[indx] == 1:
                        if (dist[origin][left] + dist[tempTour[-2]][origin] + dist[left][tempTour[2]])* RATE + filled_cap <= cap:
                            current_tour = tempTour
                            checking = False
                            filled_cap += dist[left][tempTour[2]] * RATE

                    elif checkTimeWindow2(tempTour,dist,service_time,earliest,latest) and insertList[indx] == -1:
                        if (dist[origin][tempTour[1]] + dist[left][origin] + dist[left][tempTour[-3]]) * RATE + filled_cap <= cap:
                            current_tour = tempTour
                            checking = False
                            filled_cap += dist[left][tempTour[-3]] * RATE
                            
                    indx += 1

            elif left in visited_nodes and (right not in current_tour) and current_tour != []:

                indx = 0
                checking = True
                insertList = [1 ,-1]
                while indx < len(insertList) and checking:

                    tempTour = copy.copy(current_tour)
                    tempTour.insert(insertList[indx], right)

                    if checkTimeWindow2(tempTour,dist,service_time,earliest,latest) and insertList[indx] == 1:
                        if (dist[origin][right] + dist[tempTour[-2]][origin] + dist[right][tempTour[2]])* RATE + filled_cap <= cap:
                            current_tour = tempTour
                            checking = False
                            filled_cap += dist[right][tempTour[2]] * RATE

                    elif checkTimeWindow2(tempTour,dist,service_time,earliest,latest) and insertList[indx] == -1:
                        if (dist[origin][tempTour[1]] + dist[right][origin] + dist[right][tempTour[-3]]) * RATE + filled_cap <= cap:
                            current_tour = tempTour
                            checking = False
                            filled_cap += dist[right][tempTour[-3]] * RATE
                            
                    indx += 1

            
            #If right node is visited but left node is not visited            
            elif right in visited_nodes and (left not in current_tour) and current_tour == []:

                temp_tour = [origin, left, origin]

                if checkTimeWindow2(temp_tour, dist,service_time,earliest,latest):
                    current_tour = [origin, left, origin]

            #If right node is visited but left node is not visited            
            elif left in visited_nodes and (right not in current_tour) and current_tour == []:

                temp_tour = [origin, right, origin]

                if checkTimeWindow2(temp_tour, dist,service_time,earliest,latest):
                    current_tour = [origin, right, origin]  

            #If both left and right nodes are not visited
            elif left not in visited_nodes and right not in visited_nodes:

                #Check if current tour is empty and the pair suits for battery capacity and time window
                if current_tour == [] and (dist[origin][left] + dist[left][right] + dist[right][origin])*RATE <= cap and checkTimeWindow2([origin, left, right, origin], dist, service_time, earliest, latest):
                
                    current_tour = [origin, left, right, origin]
                    filled_cap = dist[left][right] * RATE
            
                #Check if current tour is empty and pair do not fit time window, check for single node to fit in time window
                elif current_tour == [] and checkTimeWindow2([origin, right, origin], dist, service_time, earliest, latest):
                
                    current_tour = [origin, right, origin]

                elif current_tour == [] and checkTimeWindow2([origin, left, origin], dist, service_time, earliest, latest):
                
                    current_tour = [origin, left, origin]

                #If current tour is not empty
                elif current_tour != []:

                    if left in current_tour and right in current_tour:
                        pass

                    elif right == current_tour[1] and (dist[origin][left] + dist[current_tour[-2]][origin] + dist[left][right]) * RATE + filled_cap <= cap and checkTimeWindow(left, right, current_tour, earliest, latest, dist, service_time):
                        current_tour.insert(1, left)
                        filled_cap += dist[left][right] * RATE

                    elif left == current_tour[-2] and  (dist[origin][current_tour[1]] + dist[left][right] + dist[right][origin]) * RATE + filled_cap <= cap and checkTimeWindow(left, right, current_tour, earliest, latest, dist, service_time):
                        current_tour.insert(-1, right)
                        filled_cap += dist[left][right] * RATE
                    else:
                        pass
                                
        if current_tour != []:

            for node in current_tour:
                visited_nodes.add(node)
            
            tours.append(current_tour)

    counter = 0

    #Check for early arriving
    while len(visited_nodes) +  len(visited_nodes_2) != len(customers) and counter < len(customers):

        counter += 1
        print(counter)
        current_tour: list = []
        filled_cap = 0

        for (left, right) in savings:

            #If pair is visited
            if left in visited_nodes and right in visited_nodes:
                pass
            
            #If left is not visited while right is visited
            if left in visited_nodes and right not in visited_nodes and right not in visited_nodes_2:
                if current_tour == [] and checkTimeWindow2_waiting([origin, right, origin], dist, service_time, earliest, latest):
                    current_tour = [origin, right, origin]
                    print("Pairs: ", (left, right))

                elif current_tour != [] and (filled_cap + (dist[right][origin] + dist[right][current_tour[-2]] + dist[origin][current_tour[1]])*RATE <= cap)  and checkTimeWindow2_waiting(current_tour[:-1] + [right, origin] , dist, service_time, earliest, latest):
                    current_tour = current_tour[:-1] + [right, origin]
            
            #If right is not visited while left is visited
            if left not in visited_nodes and right in visited_nodes and left not in visited_nodes_2:
                if current_tour == [] and checkTimeWindow2_waiting([origin, left, origin], dist, service_time, earliest, latest):
                    current_tour = [origin, left, origin]
                    print("Pairs: ", (left, right))

                elif current_tour != [] and (filled_cap + (dist[left][origin] + dist[left][current_tour[-2]] + dist[origin][current_tour[1]])*RATE <= cap) and checkTimeWindow2_waiting(current_tour[:-1] + [left, origin] , dist, service_time, earliest, latest):
                    current_tour = current_tour[:-1] + [left, origin]
                    filled_cap += dist[left][current_tour[-2]] * RATE
            
            #If left and right are not visited
            if left not in visited_nodes and right not in visited_nodes and left not in visited_nodes_2 and right not in visited_nodes_2:
                

                
                if current_tour == [] and checkTimeWindow2_waiting([origin, left, right, origin], dist, service_time, earliest, latest) and dist[origin][left] + dist[left][right] + dist[right][origin]*RATE <= cap:
                    current_tour = [origin, left, right, origin]
                    filled_cap = dist[left][right]*RATE


                elif current_tour == [] and checkTimeWindow2_waiting([origin, right, origin], dist, service_time, earliest, latest) and dist[origin][right] + dist[right][origin]*RATE <= cap:
                
                    current_tour = [origin, right, origin]


                elif current_tour == [] and checkTimeWindow2_waiting([origin, left, origin], dist, service_time, earliest, latest) and dist[origin][left] + dist[left][origin]*RATE <= cap:
                
                    current_tour = [origin, left, origin]
            


                elif current_tour != []:
                    pass


        #If tour is not empty, mark it's nodes among visited nodes
        if current_tour != []:
            for node in current_tour:
                visited_nodes_2.add(node)
            tours.append(current_tour)

    print(visited_nodes_2)

    # Tour length computations
    total_length = 0
    tours_length = []
    tour_demands = []
    
    for tour in tours:

        if tour != []:
            demand_tour = tour_length = 0

            print(f"{tours.index(tour)+1}. Tour Visit Times")
            printTimeWindow(tour, dist, service_time, earliest, latest)

            for i in range(len(tour)-1):

                tour_length += dist[tour[i]][tour[i+1]]
                total_length += dist[tour[i]][tour[i+1]]

                if i != 0:

                    demand_tour += (dist[tour[i-1]][tour[i]] * RATE)

            tour_demands.append(demand_tour)
            tours_length.append(tour_length)
            print("-"*50)
            
    return tours, tours_length, total_length, tour_demands, eliminated_nodes, visited_nodes

# Read excel
df = pd.read_excel('rc108_21.xlsx')

# List of nodes
nodes = list(range(1,df.shape[0]))
nodes_with_depot = [0] + nodes
latitude = np.array(list(df.iloc[0:,1]))/10        #list of latitudes
longitude = np.array(list(df.iloc[0:,2]))/10       #list of longitudes
earliest = np.array(list(df.iloc[0:,3]))/10        #list of earliest times for visit
latest = np.array(list(df.iloc[0:,4]))/10          #list of latest times for visit
service_time = np.array(list(df.iloc[0:,5]))/10    #list of service times for nodes

# Distance matrix
dist = np.array([[euclidean(latitude[i], longitude[i], latitude[j], longitude[j]) 
                  for j in nodes_with_depot] 
                  for i in nodes_with_depot]
                )


#Check which nodes should be visited first
customers = {i for i in nodes if i != ORIGIN}

print("----------Possible Beginning Nodes----------")
for customer in customers:
    current_time = 0
    tour = [ORIGIN, customer, ORIGIN]

    if checkTimeWindow2(tour, dist, service_time, earliest, latest):

        print(f"Node: {tour[0]} , Current Time: {current_time}")
        for i in range(1 , len(tour)):
            current_time += (dist[tour[i-1]][tour[i]] / SPEED) + service_time[tour[i-1]]
            print(f"Node: {tour[i]} , Current Time: {current_time}, Earliest: {earliest[tour[i]]}")

        print("-"*50)


# Main Call
tours, tours_length, total_length, tour_demands, eliminated_nodes, visited_nodes = savings(nodes, ORIGIN, dist, CAP, earliest, latest, service_time)

#Printing each tour length
for i in range(len(tours)):
    print(f"{i+1}. Tour is: {tours[i]}.\nTour Length: {tours_length[i]}.\nTour consumption: {tours_length[i]*RATE}\n")

#Printing total tour lenght of all tours
print("Total Length:", total_length)

#Plotting tour routes
plt.plot(latitude[0], longitude[0], c='r', marker='s')


timely_visitedNodes = []
for tour in tours:
    timely_visitedNodes += tour
timely_visitedNodes = set(timely_visitedNodes)


for i in nodes:
    if i in eliminated_nodes:
        plt.scatter(latitude[i], longitude[i], c='k')
    elif (i in visited_nodes) and (i not in eliminated_nodes):
        plt.scatter(latitude[i], longitude[i], c='b')
    elif i not in timely_visitedNodes:
        plt.scatter(latitude[i], longitude[i], c='brown')

for indx, tour in enumerate(tours):

    for i in range(0, len(tour)-1):
    
        plt.plot([latitude[tour[i]], latitude[tour[i+1]]], [longitude[tour[i]], longitude[tour[i+1]]], c=COLORS2[ indx % len(COLORS2) ], zorder=0)

plt.show()


#---------------------------------------------------- 2 OPT ALGORITHM ----------------------------------------------------#

def two_opt(tour, tour_length, d):

    current_tour, current_tour_length = tour, tour_length
    best_tour, best_tour_length = current_tour, current_tour_length
    solution_improved = True
    
    while solution_improved:
      
             
        solution_improved = False
        for i in range(1, len(current_tour)-2):
            for j in range(i+1, len(current_tour)-1):
                difference = round((d[current_tour[i-1]][current_tour[j]]
                                  + d[current_tour[i]][current_tour[j+1]]
                                  - d[current_tour[i-1]][current_tour[i]]
                                  - d[current_tour[j]][current_tour[j+1]]), 2)
                
                
                ##########################################################################
                # In this part of the code, we have to control the revered arcs' distances 
                ##########################################################################
                
                
                ##########################################################################
                # Initially extract the subtour from i to j in the current tour 
                # and use a for loop to calculate the subtour length 
                ##########################################################################
                Before_Reverse_length = 0
                tourBefpreReverse=list(current_tour[i:j+1])                          
                for q in range(0,len(tourBefpreReverse)-1):
                    Before_Reverse_length=Before_Reverse_length+d[tourBefpreReverse[q]][tourBefpreReverse[q+1]]
                
            
                ##########################################################################
                # Now, similar to the previous case, extract the reversed subtour 
                # and calculate its length 
                ##########################################################################
                After_Reverse_length = 0
                tourAfterReverse=list(reversed(current_tour[i:j+1]))               
                for q in range(0,len(tourAfterReverse)-1):
                    After_Reverse_length=After_Reverse_length+d[tourAfterReverse[q]][tourAfterReverse[q+1]]
             
                ########################################################################################################
                # make sure that you clean these tours after each iteration and make them empty for the next iterations
                ########################################################################################################             
                while tourBefpreReverse:
                    tourBefpreReverse.pop()
                    
                while tourAfterReverse:
                    tourAfterReverse.pop()
                
                
                #################################################################################### 
                # Finally, update the "difference" value 
                # by adding the reversed subtour length and subtracting the current subtour     
                #################################################################################### 
                difference=difference - Before_Reverse_length+ After_Reverse_length
                
                        
                if current_tour_length + difference < best_tour_length and checkTimeWindow2(current_tour[:i] + list(reversed(current_tour[i:j+1])) + current_tour[j+1:], d, service_time, earliest, latest):

                    best_tour = current_tour[:i] + list(reversed(current_tour[i:j+1])) + current_tour[j+1:]
                    best_tour_length = round(current_tour_length + difference, 2)
                    solution_improved = True
                    
        current_tour, current_tour_length = best_tour, best_tour_length
    
    # Return the resulting tour and its length as a tuple
    return best_tour, best_tour_length  


for i in range(0,len(nodes_with_depot)):
    for j in range(i+1, len(nodes)):
        dist[i][j] *= -1

new_tours = list()

for tour, tour_length in zip(tours, tours_length):

    new_tour, new_tour_length = two_opt(tour, tour_length, dist)
    new_tours.append(new_tour)

    # Print the best solution found as a result of the improvement phase
    print('Improved VRP tour is', new_tour, 'with total length', new_tour_length)


#Plotting tour routes
plt.plot(latitude[0], longitude[0], c='r', marker='s')

timely_visitedNodes = []
for tour in tours:
    timely_visitedNodes += tour
timely_visitedNodes = set(timely_visitedNodes)

for i in nodes:
    if i in eliminated_nodes:
        plt.scatter(latitude[i], longitude[i], c='k')
    elif (i in visited_nodes) and (i not in eliminated_nodes):
        plt.scatter(latitude[i], longitude[i], c='b')
    elif i not in timely_visitedNodes:
        plt.scatter(latitude[i], longitude[i], c='brown')

for indx, tour in enumerate(new_tours):
    for i in range(0, len(tour)-1):
    
        plt.plot([latitude[tour[i]], latitude[tour[i+1]]], [longitude[tour[i]], longitude[tour[i+1]]], c=COLORS2[ indx % len(COLORS2) ], zorder=0)

plt.show()
